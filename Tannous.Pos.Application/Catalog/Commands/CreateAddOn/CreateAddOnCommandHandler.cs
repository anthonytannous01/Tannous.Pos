using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.CreateAddOn;

public class CreateAddOnCommandHandler : IRequestHandler<CreateAddOnCommand, AddOnDto>
{
    private readonly IAddOnRepository _addOnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAddOnCommandHandler(IAddOnRepository addOnRepository, IUnitOfWork unitOfWork)
    {
        _addOnRepository = addOnRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddOnDto> Handle(CreateAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = new AddOn
        {
            Name = request.AddOn.Name,
            Description = request.AddOn.Description,
            Price = request.AddOn.Price,
            IsActive = request.AddOn.IsActive
        };

        await _addOnRepository.AddAsync(addOn);
        await _unitOfWork.SaveChangesAsync();

        return new AddOnDto
        {
            Id = addOn.Id,
            Name = addOn.Name,
            Description = addOn.Description,
            Price = addOn.Price,
            IsActive = addOn.IsActive,
            CreatedAt = addOn.CreatedAt
        };
    }
}
