using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

/// <summary>
/// Process-local bounded governance fingerprint history (not persisted; not business telemetry storage).
/// GOVERNANCE: retains at most one previous fingerprint per snapshot key.
/// </summary>
public sealed class OperationalGovernanceFingerprintHistoryStore
{
    private readonly object _sync = new();
    private FingerprintEntry? _previous;
    private FingerprintEntry? _current;

    public OperationalGovernanceFingerprintComparisonDto RecordBuild(
        OperationalGovernanceSnapshotComposition composition,
        IOperationalDiagnosticsCacheTelemetry telemetry)
    {
        var comparison = BuildComparison(composition);

        lock (_sync)
        {
            if (_current != null
                && !string.Equals(_current.FingerprintHash, composition.FingerprintHash, StringComparison.Ordinal))
            {
                _previous = _current;
                if (_previous != null && OperationalGovernanceFingerprintConstants.MaxPreviousFingerprintEntries == 1)
                {
                    // bounded single previous entry
                }

                telemetry.RecordGovernanceFingerprintTransition();

                if (Enum.TryParse<OperationalGovernanceDriftDirection>(
                        comparison.DriftDirection,
                        out var drift)
                    && drift is OperationalGovernanceDriftDirection.Degrading
                        or OperationalGovernanceDriftDirection.Oscillating)
                    telemetry.RecordGovernanceDriftEscalation();
            }
            else if (_current != null
                && string.Equals(_current.FingerprintHash, composition.FingerprintHash, StringComparison.Ordinal))
            {
                telemetry.RecordGovernanceStableFingerprintHit();
            }

            _current = new FingerprintEntry(
                composition.FingerprintHash,
                composition.NormalizedSignature,
                composition.SignatureSegments);
        }

        return comparison;
    }

    public OperationalGovernanceFingerprintComparisonDto GetCurrentComparison(
        OperationalGovernanceSnapshotComposition composition)
    {
        lock (_sync)
            return BuildComparison(composition);
    }

    public void InvalidateAll()
    {
        lock (_sync)
        {
            _previous = null;
            _current = null;
        }
    }

    private OperationalGovernanceFingerprintComparisonDto BuildComparison(
        OperationalGovernanceSnapshotComposition composition)
    {
        FingerprintEntry? previous;
        lock (_sync)
            previous = _previous;

        return OperationalGovernanceFingerprintComparer.Compare(
            composition.FingerprintHash,
            previous?.FingerprintHash,
            composition.SignatureSegments,
            previous?.SignatureSegments);
    }

    private sealed record FingerprintEntry(
        string FingerprintHash,
        string NormalizedSignature,
        IReadOnlyList<string> SignatureSegments);
}
