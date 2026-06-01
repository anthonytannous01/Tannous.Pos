using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class SyncCursor : BaseEntity
{
    public string DeviceId { get; set; } = string.Empty;
    public string Cursor { get; set; } = string.Empty;
    public DateTime LastSyncAt { get; set; }
}
