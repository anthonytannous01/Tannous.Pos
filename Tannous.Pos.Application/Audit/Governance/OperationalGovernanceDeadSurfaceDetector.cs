namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Detects advisory dead or redundant governance surface without auto-deletion.</summary>
public static class OperationalGovernanceDeadSurfaceDetector
{
    private static readonly string[] KnownOrphanServiceMethods =
    [
        "GetCardinalitySnapshotAsync",
        "GetDegradationAsync",
        "GetScopeDiagnosticsAsync"
    ];

    public static OperationalGovernanceDeadSurfaceDetectionResult Detect(string repositoryRoot)
    {
        var findings = new List<string>();

        var servicePath = Path.Combine(
            repositoryRoot,
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsCacheDiagnosticsService.cs");
        var controllerPath = Path.Combine(
            repositoryRoot,
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");

        if (File.Exists(servicePath) && File.Exists(controllerPath))
        {
            var service = File.ReadAllText(servicePath);
            var controller = File.ReadAllText(controllerPath);

            foreach (var method in KnownOrphanServiceMethods)
            {
                if (service.Contains(method, StringComparison.Ordinal)
                    && !ControllerReferencesServiceMethod(controller, method))
                    findings.Add($"OrphanServiceMethod:{method}");
            }
        }

        var runtimeBuilderPath = Path.Combine(
            repositoryRoot,
            "Tannous.Pos.Application",
            "Audit",
            "Governance",
            "OperationalGovernanceRuntimeProtectionBuilder.cs");
        var executionBuilderPath = Path.Combine(
            repositoryRoot,
            "Tannous.Pos.Application",
            "Audit",
            "Governance",
            "OperationalGovernanceExecutionDiagnosticsBuilder.cs");

        if (File.Exists(runtimeBuilderPath) && File.Exists(executionBuilderPath))
        {
            var runtimeBuilder = File.ReadAllText(runtimeBuilderPath);
            if (runtimeBuilder.Contains("BuildExecutionDiagnostics(", StringComparison.Ordinal)
                && runtimeBuilder.Contains("OperationalGovernanceExecutionDiagnosticsBuilder.Build", StringComparison.Ordinal))
                findings.Add("DuplicateExecutionDiagnosticsPath:RuntimeProtectionBuilder");
        }

        var snapshotStorePath = Path.Combine(
            repositoryRoot,
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections",
            "OperationalGovernanceSnapshotStore.cs");
        if (File.Exists(snapshotStorePath))
        {
            var store = File.ReadAllText(snapshotStorePath);
            if (store.Contains("IServiceProvider", StringComparison.Ordinal))
                findings.Add("BroadDependency:SnapshotStoreServiceProvider");
        }

        return new OperationalGovernanceDeadSurfaceDetectionResult
        {
            Findings = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(findings, 8)
        };
    }

    private static bool ControllerReferencesServiceMethod(string controller, string serviceMethodName)
    {
        var routeStem = serviceMethodName
            .Replace("Get", string.Empty, StringComparison.Ordinal)
            .Replace("Async", string.Empty, StringComparison.Ordinal);

        return controller.Contains(serviceMethodName, StringComparison.Ordinal)
            || controller.Contains(ToKebabCase(routeStem), StringComparison.OrdinalIgnoreCase);
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var chars = new List<char> { char.ToLowerInvariant(value[0]) };
        for (var i = 1; i < value.Length; i++)
        {
            if (char.IsUpper(value[i]))
            {
                chars.Add('-');
                chars.Add(char.ToLowerInvariant(value[i]));
            }
            else
            {
                chars.Add(value[i]);
            }
        }

        return new string(chars.ToArray());
    }
}
