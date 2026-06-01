using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.UpdateAddOn;

public class UpdateAddOnCommandHandler : IRequestHandler<UpdateAddOnCommand, AddOnDto>
{
    private readonly IAddOnRepository _addOnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAddOnCommandHandler(IAddOnRepository addOnRepository, IUnitOfWork unitOfWork)
    {
        _addOnRepository = addOnRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddOnDto> Handle(UpdateAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = await _addOnRepository.GetByIdAsync(request.Id);
        if (addOn == null)
            throw new ArgumentException($"Add-on with ID {request.Id} not found");

        addOn.Name = request.AddOn.Name;
        addOn.Description = request.AddOn.Description;
        addOn.Price = request.AddOn.Price;
        addOn.IsActive = request.AddOn.IsActive;
        addOn.UpdatedAt = DateTime.UtcNow;

        await _addOnRepository.UpdateAsync(addOn);
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
