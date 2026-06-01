using Tannous.Pos.Domain.Common;

namespace Tannous.Pos.Domain.Entities;

public class ReceiptSequence : BaseEntity, IAggregateRoot
{
    public string SequenceType { get; set; } = string.Empty; // Order, Receipt, etc.
    public string Prefix { get; set; } = string.Empty;
    public int CurrentNumber { get; set; }
    public int NextNumber { get; set; }
    public string? Suffix { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastUsed { get; set; }
}
