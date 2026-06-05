using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;
using Tannous.Pos.Application.Reservations.Queries.GetReservations;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Reservations.Commands.CreateReservation;

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, ReservationDto>
{
    private readonly IReservationRepository _repo;
    private readonly IBranchRepository      _branchRepo;

    public CreateReservationCommandHandler(
        IReservationRepository repo,
        IBranchRepository      branchRepo)
    {
        _repo       = repo;
        _branchRepo = branchRepo;
    }

    public async Task<ReservationDto> Handle(
        CreateReservationCommand request, CancellationToken ct)
    {
        // Resolve branch — explicit → default
        var branchId = request.BranchId
            ?? (await _branchRepo.GetDefaultAsync(ct))?.Id;

        // Mark the table as Reserved if one was assigned
        var reservation = new Reservation
        {
            CustomerName        = request.CustomerName.Trim(),
            CustomerPhone       = request.CustomerPhone?.Trim(),
            PartySize           = request.PartySize,
            ReservationDateTime = request.ReservationDateTime,
            Notes               = request.Notes?.Trim(),
            Status              = ReservationStatus.Pending,
            TableId             = request.TableId,
            BranchId            = branchId
        };

        await _repo.AddAsync(reservation, ct);
        await _repo.CommitAsync(ct);

        return GetReservationsQueryHandler.Map(reservation);
    }
}
