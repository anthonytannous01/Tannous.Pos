using MediatR;
using Tannous.Pos.Application.DTOs.Reservations;
using Tannous.Pos.Application.Reservations.Queries.GetReservations;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Reservations.Commands.UpdateReservationStatus;

public class UpdateReservationStatusCommandHandler
    : IRequestHandler<UpdateReservationStatusCommand, ReservationDto>
{
    private readonly IReservationRepository _repo;

    public UpdateReservationStatusCommandHandler(IReservationRepository repo) => _repo = repo;

    public async Task<ReservationDto> Handle(
        UpdateReservationStatusCommand request, CancellationToken ct)
    {
        var reservation = await _repo.GetByIdAsync(request.ReservationId, ct)
            ?? throw new InvalidOperationException($"Reservation {request.ReservationId} not found.");

        reservation.Status    = request.NewStatus;
        reservation.UpdatedAt = DateTime.UtcNow;

        if (request.TableId.HasValue)
            reservation.TableId = request.TableId;

        await _repo.CommitAsync(ct);

        return GetReservationsQueryHandler.Map(reservation);
    }
}
