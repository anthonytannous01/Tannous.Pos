using MediatR;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Application.Delivery.Queries.GetDeliveryQueue;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Delivery.Commands.CreateDeliveryInfo;

public class CreateDeliveryInfoCommandHandler : IRequestHandler<CreateDeliveryInfoCommand, DeliveryDto>
{
    private readonly IDeliveryRepository _repo;
    private readonly IBranchRepository   _branchRepo;

    public CreateDeliveryInfoCommandHandler(
        IDeliveryRepository repo,
        IBranchRepository   branchRepo)
    {
        _repo       = repo;
        _branchRepo = branchRepo;
    }

    public async Task<DeliveryDto> Handle(
        CreateDeliveryInfoCommand request, CancellationToken ct)
    {
        // Ensure no duplicate delivery info for this order
        var existing = await _repo.GetByOrderIdAsync(request.OrderId, ct);
        if (existing != null)
            throw new InvalidOperationException(
                $"DeliveryInfo already exists for order {request.OrderId}.");

        var branchId = request.BranchId
            ?? (await _branchRepo.GetDefaultAsync(ct))?.Id;

        var delivery = new DeliveryInfo
        {
            OrderId          = request.OrderId,
            DeliveryAddress  = request.DeliveryAddress.Trim(),
            ApartmentDetails = request.ApartmentDetails?.Trim(),
            CustomerPhone    = request.CustomerPhone?.Trim(),
            Channel          = (DeliveryChannel)request.Channel,
            Status           = DeliveryStatus.Pending,
            DeliveryFee      = request.DeliveryFee,
            EstimatedMinutes = request.EstimatedMinutes,
            Notes            = request.Notes?.Trim(),
            BranchId         = branchId
        };

        await _repo.AddAsync(delivery, ct);
        await _repo.CommitAsync(ct);

        return GetDeliveryQueueQueryHandler.Map(delivery);
    }
}
