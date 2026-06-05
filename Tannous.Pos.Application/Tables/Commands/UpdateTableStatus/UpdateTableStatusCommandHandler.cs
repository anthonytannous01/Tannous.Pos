using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Tables;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Tables.Commands.UpdateTableStatus;

public class UpdateTableStatusCommandHandler : IRequestHandler<UpdateTableStatusCommand, TableDto>
{
    private readonly DbContext _dbContext;

    public UpdateTableStatusCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<TableDto> Handle(UpdateTableStatusCommand request, CancellationToken cancellationToken)
    {
        var table = await _dbContext.Set<Table>()
            .Include(t => t.FloorPlan)
            .FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken)
            ?? throw new InvalidOperationException($"Table {request.TableId} not found");

        table.Status    = request.NewStatus;
        table.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TableDto
        {
            Id            = table.Id,
            TableNumber   = table.TableNumber,
            Label         = table.Label,
            Capacity      = table.Capacity,
            Status        = table.Status,
            IsActive      = table.IsActive,
            DisplayOrder  = table.DisplayOrder,
            FloorPlanId   = table.FloorPlanId,
            FloorPlanName = table.FloorPlan.Name
        };
    }
}
