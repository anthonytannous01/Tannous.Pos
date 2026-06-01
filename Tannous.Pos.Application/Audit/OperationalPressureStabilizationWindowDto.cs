namespace Tannous.Pos.Application.Audit;

public sealed class OperationalPressureStabilizationWindowDto
{
    public string WindowClassification { get; init; } = string.Empty;
    public bool StabilizationActive { get; init; }
    public bool ChurnReboundDetected { get; init; }
    public long RecoveryWindowExtensions { get; init; }
    public long StabilizationWindowResets { get; init; }
    public long PressureRecoveryCycles { get; init; }
    public IReadOnlyList<string> StabilizationSignals { get; init; } = Array.Empty<string>();
}
