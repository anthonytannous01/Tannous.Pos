using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class IdempotentRequest : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ResponseHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
