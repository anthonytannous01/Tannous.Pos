using MediatR;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Application.Shifts.Commands.OpenShift;

public class OpenShiftCommandHandler : IRequestHandler<OpenShiftCommand, ShiftDto>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IReceiptNumberService _receiptNumberService;
    private readonly IUnitOfWork _unitOfWork;

    public OpenShiftCommandHandler(
        IShiftRepository shiftRepository,
        IReceiptNumberService receiptNumberService,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _receiptNumberService = receiptNumberService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftDto> Handle(OpenShiftCommand request, CancellationToken cancellationToken)
    {
        // Check if user already has an open shift
        var existingShift = await _shiftRepository.GetOpenShiftByUserAsync(request.UserId);
        if (existingShift != null)
            throw new InvalidOperationException("User already has an open shift");

        var shiftNumber = await _receiptNumberService.GenerateShiftNumberAsync();

        var shift = new Shift
        {
            ShiftNumber = shiftNumber,
            StartTime = DateTime.UtcNow,
            OpeningBalance = request.OpeningBalance,
            ExpectedCash = request.OpeningBalance,
            Status = ShiftStatus.Open,
            Notes = request.Notes,
            UserId = request.UserId,
            CreatedBy = request.UserId.ToString()
        };

        await _shiftRepository.AddAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        return new ShiftDto
        {
            Id = shift.Id,
            ShiftNumber = shift.ShiftNumber,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            OpeningBalance = shift.OpeningBalance,
            ClosingBalance = shift.ClosingBalance,
            ExpectedCash = shift.ExpectedCash,
            ActualCash = shift.ActualCash,
            CashDifference = shift.CashDifference,
            Status = shift.Status.ToString(),
            Notes = shift.Notes,
            UserId = shift.UserId,
            CreatedAt = shift.CreatedAt
        };
    }
}
