using System.Text.RegularExpressions;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Runtime integrity: optimistic concurrency → 409 ProblemDetails, warning-only observability, money-path visibility anchors.
/// </summary>
public class ConcurrencyConflictHandlingGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void GlobalExceptionHandler_maps_DbUpdateConcurrencyException_to_409_ProblemDetails_with_correlationId()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.WebApi", "Middleware", "GlobalExceptionHandler.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("DbUpdateConcurrencyException", text, StringComparison.Ordinal);
        Assert.Contains("case DbUpdateConcurrencyException:", text, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status409Conflict", text, StringComparison.Ordinal);
        Assert.Contains("\"Concurrency conflict\"", text, StringComparison.Ordinal);
        Assert.Contains("Status = statusCode", text, StringComparison.Ordinal);
        Assert.Contains("Extensions[\"correlationId\"]", text, StringComparison.Ordinal);
        Assert.Contains("LogWarning", text, StringComparison.Ordinal);
        Assert.Contains("AffectedEntityTypes", text, StringComparison.Ordinal);
        Assert.True(
            Regex.IsMatch(text, @"LogWarning\s*\(\s*concurrencyEx", RegexOptions.Singleline),
            "Expected DbUpdateConcurrencyException branch to log at Warning level with the exception instance.");
    }

    [Fact]
    public void FinalizeOrderCommandHandler_documents_money_path_concurrency_warning()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder", "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Money-path concurrency visibility: optimistic concurrency conflict during finalize", text, StringComparison.Ordinal);
        Assert.Contains("ConcurrencyConflictObservability.FormatAffectedClrTypeNames", text, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidOrderCommandHandler_documents_money_path_concurrency_warning()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "VoidOrder", "VoidOrderCommandHandler.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Money-path concurrency visibility: optimistic concurrency conflict during void", text, StringComparison.Ordinal);
        Assert.Contains("ConcurrencyConflictObservability.FormatAffectedClrTypeNames", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ConcurrencyConflictObservability_helper_retained_for_handler_logs()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Application", "Orders", "ConcurrencyConflictObservability.cs");
        Assert.True(File.Exists(path), "Expected ConcurrencyConflictObservability.cs for shared affected-entity formatting.");
        var text = File.ReadAllText(path);
        Assert.Contains("FormatAffectedClrTypeNames", text, StringComparison.Ordinal);
        Assert.Contains("DbUpdateConcurrencyException", text, StringComparison.Ordinal);
    }
}
