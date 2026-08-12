using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Shifts.Commands.OpenShift;

public class OpenShiftCommandHandler : IRequestHandler<OpenShiftCommand, ShiftDto>
{
    private readonly IShiftRepository           _shiftRepository;
    private readonly IReceiptNumberService       _receiptNumberService;
    private readonly IUnitOfWork                 _unitOfWork;
    private readonly DbContext                   _dbContext;
    private readonly IBranchRepository           _branchRepository;
    private readonly IBusinessSettingsRepository _settingsRepository;

    public OpenShiftCommandHandler(
        IShiftRepository           shiftRepository,
        IReceiptNumberService       receiptNumberService,
        IUnitOfWork                 unitOfWork,
        DbContext                   dbContext,
        IBranchRepository           branchRepository,
        IBusinessSettingsRepository settingsRepository)
    {
        _shiftRepository     = shiftRepository;
        _receiptNumberService = receiptNumberService;
        _unitOfWork           = unitOfWork;
        _dbContext            = dbContext;
        _branchRepository     = branchRepository;
        _settingsRepository   = settingsRepository;
    }

    public async Task<ShiftDto> Handle(OpenShiftCommand request, CancellationToken cancellationToken)
    {
        // NpgsqlRetryingExecutionStrategy (registered via EnableRetryOnFailure) does not allow
        // direct BeginTransactionAsync calls. Wrap the transaction block in the execution strategy
        // so the driver can retry the entire unit on transient failures.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        ShiftDto? result = null;

        await strategy.ExecuteAsync(async () =>
        {
            // Serializable prevents phantom reads: two parallel open-shift requests cannot
            // both see "no open shift" and both succeed. The second will serialize behind
            // the first and see the newly committed shift on its own read, then reject.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var committed = false;
            try
            {
                // Re-check inside the serializable transaction snapshot.
                var existingShift = await _shiftRepository.GetOpenShiftByUserAsync(request.UserId);
                if (existingShift != null)
                    throw new InvalidOperationException("User already has an open shift");

                var shiftNumber = await _receiptNumberService.GenerateShiftNumberAsync();

                // Resolve branch: use the one from the request, or fall back to the default branch
                var branchId = request.BranchId;
                if (branchId == null)
                {
                    var settings = await _settingsRepository.GetAsync(cancellationToken);
                    branchId = settings?.DefaultBranchId
                        ?? (await _branchRepository.GetDefaultAsync(cancellationToken))?.Id;
                }

                var shift = new Shift
                {
                    ShiftNumber    = shiftNumber,
                    StartTime      = DateTime.UtcNow,
                    OpeningBalance = request.OpeningBalance,
                    ExpectedCash   = request.OpeningBalance,
                    OpeningBalanceLbp = request.OpeningBalanceLbp,
                    ExpectedCashLbp   = request.OpeningBalanceLbp,
                    Status         = ShiftStatus.Open,
                    Notes          = request.Notes,
                    UserId         = request.UserId,
                    BranchId       = branchId,
                    CreatedBy      = request.UserId.ToString()
                };

                await _shiftRepository.AddAsync(shift);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync(cancellationToken);
                committed = true;

                result = new ShiftDto
                {
                    Id             = shift.Id,
                    ShiftNumber    = shift.ShiftNumber,
                    StartTime      = shift.StartTime,
                    EndTime        = shift.EndTime,
                    OpeningBalance = shift.OpeningBalance,
                    ClosingBalance = shift.ClosingBalance,
                    ExpectedCash   = shift.ExpectedCash,
                    ActualCash     = shift.ActualCash,
                    CashDifference = shift.CashDifference,
                    Status         = shift.Status.ToString(),
                    Notes          = shift.Notes,
                    UserId         = shift.UserId,
                    CreatedAt      = shift.CreatedAt
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "A shift was opened concurrently. Refresh and try again.");
            }
            finally
            {
                if (!committed)
                {
                    try { await transaction.RollbackAsync(cancellationToken); }
                    catch { /* best-effort — connection may already be closed */ }
                }
            }
        });

        return result!;
    }
}
