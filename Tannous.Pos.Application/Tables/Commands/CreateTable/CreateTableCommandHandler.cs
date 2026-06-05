using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Tables;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Tables.Commands.CreateTable;

public class CreateTableCommandHandler : IRequestHandler<CreateTableCommand, TableDto>
{
    private readonly DbContext _dbContext;

    public CreateTableCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<TableDto> Handle(CreateTableCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Table;

        var floorPlan = await _dbContext.Set<FloorPlan>()
            .FindAsync(new object[] { dto.FloorPlanId }, cancellationToken)
            ?? throw new InvalidOperationException($"FloorPlan {dto.FloorPlanId} not found");

        var table = new Table
        {
            TableNumber  = dto.TableNumber.Trim(),
            Label        = dto.Label?.Trim(),
            Capacity     = dto.Capacity,
            FloorPlanId  = dto.FloorPlanId,
            DisplayOrder = dto.DisplayOrder
        };

        _dbContext.Set<Table>().Add(table);
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
            FloorPlanName = floorPlan.Name
        };
    }
}
