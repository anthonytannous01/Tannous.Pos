namespace Tannous.Pos.Application.Audit;

public sealed class OperationalReplayRiskSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int TotalReplayReceiptCount { get; init; }
    public int MaxReceiptsOnSingleDevice { get; init; }
    public int ReplayMismatchUnresolvedCount { get; init; }
    public bool ReplayStormRiskIndicated { get; init; }
    public string ReplayRiskClassification { get; init; } = "Normal";
    public IReadOnlyDictionary<string, string> Guidance { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
