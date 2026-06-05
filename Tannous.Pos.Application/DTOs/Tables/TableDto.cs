using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Tables;

public class FloorPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<TableDto> Tables { get; set; } = new();
}

public class TableDto
{
    public Guid Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int Capacity { get; set; }
    public TableStatus Status { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public Guid FloorPlanId { get; set; }
    public string FloorPlanName { get; set; } = string.Empty;
    /// <summary>Active order ID on this table, if currently occupied.</summary>
    public Guid? ActiveOrderId { get; set; }
}

public class CreateTableDto
{
    public string TableNumber { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int Capacity { get; set; } = 2;
    public Guid FloorPlanId { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateTableStatusDto
{
    public TableStatus Status { get; set; }
}

public class CreateFloorPlanDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}
