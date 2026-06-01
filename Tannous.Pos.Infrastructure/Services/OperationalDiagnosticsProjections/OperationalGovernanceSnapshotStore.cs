using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

/// <summary>
/// Process-local governance projection reuse store (short TTL; not business cache; not persisted).
/// GOVERNANCE: lazy expiry on read; no timers or background workers.
/// </summary>
public sealed class OperationalGovernanceSnapshotStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, CachedEntry> _entries = new(StringComparer.Ordinal);
    private string? _lastConsistencyLevel;

    private readonly Lazy<IOperationalDiagnosticsCache> _cache;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly IOperationalResiliencePressureState _pressureState;
    private readonly OperationalGovernanceFingerprintHistoryStore _fingerprintHistory;

    public OperationalGovernanceSnapshotStore(
        Lazy<IOperationalDiagnosticsCache> cache,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        IOperationalResiliencePressureState pressureState,
        OperationalGovernanceFingerprintHistoryStore fingerprintHistory)
    {
        _cache = cache;
        _telemetry = telemetry;
        _pressureState = pressureState;
        _fingerprintHistory = fingerprintHistory;
    }

    public OperationalGovernanceSnapshotAccess Acquire(
        OperationalGovernanceProfile profile = OperationalGovernanceProfile.Standard)
    {
        var key = OperationalGovernanceSnapshotKeys.ForProfile(profile);
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            if (_entries.TryGetValue(key, out var cached))
            {
                var age = (now - cached.CreatedUtc).TotalSeconds;
                if (age < OperationalGovernanceSnapshotReuseConstants.TtlSeconds)
                {
                    _telemetry.RecordGovernanceSnapshotReuse();
                    _telemetry.RecordProjectionReuseHit();
                    return CreateAccess(cached.Composition, wasReused: true, age, isExpired: false);
                }
            }
        }

        var composition = BuildComposition(key, profile);
        NoteConsistencyTransition(composition);
        var comparison = _fingerprintHistory.RecordBuild(composition, _telemetry);
        NoteFragmentation(comparison);

        lock (_sync)
        {
            _entries[key] = new CachedEntry(composition, composition.CreatedUtc);
            EnforceKeyBudget();
        }

        _telemetry.RecordGovernanceSnapshotBuild();
        _telemetry.RecordProjectionReuseMiss();

        return CreateAccess(composition, wasReused: false, ageSeconds: 0, isExpired: false);
    }

    public void InvalidateAll()
    {
        lock (_sync)
            _entries.Clear();

        _fingerprintHistory.InvalidateAll();
    }

    private void NoteFragmentation(OperationalGovernanceFingerprintComparisonDto comparison)
    {
        if (string.Equals(
                comparison.FingerprintStability,
                OperationalGovernanceFingerprintStability.Fragmented.ToString(),
                StringComparison.Ordinal))
            _telemetry.RecordProjectionFragmentationSignal();
    }

    private OperationalGovernanceSnapshotComposition BuildComposition(
        string key,
        OperationalGovernanceProfile profile)
    {
        var buildStarted = Environment.TickCount64;
        var cache = _cache.Value;
        var context = OperationalGovernanceProjectionPipeline.Execute(
            cache.GetDiagnosticsEntryMetadata(),
            OperationalGovernanceTelemetryAccess.CaptureSnapshot(_telemetry),
            _pressureState,
            profile);

        var buildElapsed = (int)Math.Min(int.MaxValue, Environment.TickCount64 - buildStarted);

        return OperationalGovernanceSnapshotBuilder.BuildComposition(
            key,
            profile,
            context,
            _telemetry,
            buildElapsed);
    }

    private void NoteConsistencyTransition(OperationalGovernanceSnapshotComposition composition)
    {
        var level = OperationalGovernanceProjectionConsistencyClassifier.Classify(
            OperationalGovernanceSnapshotState.Fresh,
            composition.Context,
            OperationalGovernanceProjectionReuseLevel.None,
            composition.GovernanceConsistency).ToString();

        if (_lastConsistencyLevel != null
            && !string.Equals(_lastConsistencyLevel, level, StringComparison.Ordinal))
            _telemetry.RecordSnapshotConsistencyTransition();

        _lastConsistencyLevel = level;
    }

    private void EnforceKeyBudget()
    {
        if (_entries.Count <= OperationalGovernanceSnapshotReuseConstants.MaxSnapshotKeys)
            return;

        var oldest = _entries
            .OrderBy(kvp => kvp.Value.CreatedUtc)
            .First();
        _entries.Remove(oldest.Key);
    }

    private static OperationalGovernanceSnapshotAccess CreateAccess(
        OperationalGovernanceSnapshotComposition composition,
        bool wasReused,
        double ageSeconds,
        bool isExpired) =>
        new()
        {
            Composition = composition,
            WasReused = wasReused,
            AgeSeconds = ageSeconds,
            IsExpired = isExpired
        };

    private sealed record CachedEntry(
        OperationalGovernanceSnapshotComposition Composition,
        DateTime CreatedUtc);
}
