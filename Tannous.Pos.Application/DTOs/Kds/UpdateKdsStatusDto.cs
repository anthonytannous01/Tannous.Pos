using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Kds;

public class UpdateKdsStatusDto
{
    /// <summary>The new KDS status to set on the order line.</summary>
    public KdsStatus Status { get; set; }
}
