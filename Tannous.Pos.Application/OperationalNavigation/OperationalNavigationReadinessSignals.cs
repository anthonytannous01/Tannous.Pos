namespace Tannous.Pos.Application.OperationalNavigation;

/// <summary>Minimal operator-safe readiness signals extracted upstream (no cache metadata).</summary>
public sealed class OperationalNavigationReadinessSignals
{
    public string ReadinessState { get; init; } = string.Empty;
    public string PressureSeverity { get; init; } = string.Empty;
    public string StabilityClassification { get; init; } = string.Empty;
    public bool RuntimeProtectionActive { get; init; }
}
