namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Shared measured governance surface counters for tests, scans, and audits.</summary>
public static class OperationalGovernanceSurfaceMeasurementHelper
{
    public static OperationalGovernanceCeilingMeasurement.OperationalGovernanceCeilingSnapshot MeasureFromRepository(
        string repositoryRoot)
    {
        var controllerPath = Path.Combine(
            repositoryRoot,
            "Tannous.Pos.WebApi",
            "Controllers",
            "Internal",
            "OperationalAuditCacheDiagnosticsController.cs");
        var endpointCount = File.Exists(controllerPath)
            ? System.Text.RegularExpressions.Regex.Matches(
                File.ReadAllText(controllerPath),
                @"\[HttpGet\(""").Count
            : 0;

        var projectionsDir = Path.Combine(
            repositoryRoot,
            "Tannous.Pos.Infrastructure",
            "Services",
            "OperationalDiagnosticsProjections");
        var collaboratorCount = Directory.Exists(projectionsDir)
            ? Directory.EnumerateFiles(projectionsDir, "*Collaborator*.cs").Count()
            : 0;

        var auditDir = Path.Combine(repositoryRoot, "Tannous.Pos.Application", "Audit");
        var projectionBuilderCount = CountTopLevel(auditDir, "*ProjectionBuilder*.cs");
        var classifierCount = CountTopLevel(auditDir, "*Classifier*.cs");
        var explainabilityBuilderCount = CountTopLevel(auditDir, "*ExplainabilityBuilder*.cs");
        var dtoCount = CountTopLevel(auditDir, "*Dto.cs");

        return OperationalGovernanceCeilingMeasurement.Measure(
            endpointCount,
            collaboratorCount,
            projectionBuilderCount,
            classifierCount,
            explainabilityBuilderCount,
            dtoCount);
    }

    private static int CountTopLevel(string auditDir, string pattern) =>
        Directory.Exists(auditDir)
            ? Directory.EnumerateFiles(auditDir, pattern, SearchOption.TopDirectoryOnly).Count()
            : 0;
}
