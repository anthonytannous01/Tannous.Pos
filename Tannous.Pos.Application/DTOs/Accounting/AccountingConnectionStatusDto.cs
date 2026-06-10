namespace Tannous.Pos.Application.DTOs.Accounting;

public class AccountingConnectionStatusDto
{
    public string    Provider        { get; set; } = string.Empty;
    public bool      IsConnected     { get; set; }
    public string?   CompanyName     { get; set; }
    public DateTime? LastSyncAt      { get; set; }
    public string?   LastSyncError   { get; set; }
    public int       SyncRecordCount { get; set; }
}

public class SyncTriggerResultDto
{
    public int Synced { get; set; }
    public List<string> Errors { get; set; } = new();
}
