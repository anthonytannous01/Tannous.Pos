using MediatR;
using Microsoft.EntityFrameworkCore;
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
    private readonly DbContext              _dbContext;
    private readonly INotificationService   _notificationService;

    public CreateReservationCommandHandler(
        IReservationRepository repo,
        IBranchRepository      branchRepo,
        DbContext              dbContext,
        INotificationService   notificationService)
    {
        _repo                = repo;
        _branchRepo          = branchRepo;
        _dbContext           = dbContext;
        _notificationService = notificationService;
    }

    public async Task<ReservationDto> Handle(
        CreateReservationCommand request, CancellationToken cancellationToken)
    {
        // Resolve branch — explicit → default
        var branchId = request.BranchId
            ?? (await _branchRepo.GetDefaultAsync(cancellationToken))?.Id;

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

        await _repo.AddAsync(reservation, cancellationToken);
        await _repo.CommitAsync(cancellationToken);

        var settings = await _dbContext.Set<BusinessSettings>()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings?.NotifyOnReservationConfirm == true &&
            !string.IsNullOrWhiteSpace(reservation.CustomerPhone))
        {
            string? tableName = null;
            if (reservation.TableId.HasValue)
            {
                var table = await _dbContext.Set<Table>()
                    .FindAsync(new object[] { reservation.TableId.Value }, cancellationToken);
                tableName = table?.Label ?? table?.TableNumber;
            }

            _ = _notificationService.SendReservationConfirmationAsync(
                toPhone:             reservation.CustomerPhone,
                customerName:        reservation.CustomerName,
                reservationDateTime: reservation.ReservationDateTime,
                partySize:           reservation.PartySize,
                tableName:           tableName,
                businessName:        settings.BusinessName,
                cancellationToken:   cancellationToken);
        }

        return GetReservationsQueryHandler.Map(reservation);
    }
}
