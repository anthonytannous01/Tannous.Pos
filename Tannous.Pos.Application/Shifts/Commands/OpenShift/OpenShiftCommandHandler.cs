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
    private readonly IShiftRepository _shiftRepository;
    private readonly IReceiptNumberService _receiptNumberService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DbContext _dbContext;

    public OpenShiftCommandHandler(
        IShiftRepository shiftRepository,
        IReceiptNumberService receiptNumberService,
        IUnitOfWork unitOfWork,
        DbContext dbContext)
    {
        _shiftRepository = shiftRepository;
        _receiptNumberService = receiptNumberService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task<ShiftDto> Handle(OpenShiftCommand request, CancellationToken cancellationToken)
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

            var shift = new Shift
            {
                ShiftNumber    = shiftNumber,
                StartTime      = DateTime.UtcNow,
                OpeningBalance = request.OpeningBalance,
                ExpectedCash   = request.OpeningBalance,
                Status         = ShiftStatus.Open,
                Notes          = request.Notes,
                UserId         = request.UserId,
                CreatedBy      = request.UserId.ToString()
            };

            await _shiftRepository.AddAsync(shift);
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync(cancellationToken);
            committed = true;

            return new ShiftDto
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
    }
}
