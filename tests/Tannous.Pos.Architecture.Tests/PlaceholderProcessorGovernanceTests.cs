using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// A sync processor that does no work must not report success.
///
/// ProcessOpenShift and ProcessCreateCustomer persist nothing. They previously returned
/// Success = true with "Shift opened successfully" / "Customer created successfully", which tells
/// the device the operation is done so it clears it from the outbox. Nothing would have been
/// written server-side and nothing would remain client-side.
///
/// No shipped client enqueues either type today, so this was a trap for the next person rather
/// than a live defect. These tests keep it a trap that springs immediately and visibly.
/// </summary>
public class PlaceholderProcessorGovernanceTests
{
    private static string SyncControllerSource() =>
        File.ReadAllText(Path.Combine(
            ObservabilitySourceGovernanceTests.RepoRoot(),
            "Tannous.Pos.WebApi", "Controllers", "SyncController.cs"));

    [Theory]
    [InlineData("ProcessOpenShift")]
    [InlineData("ProcessCreateCustomer")]
    public void Placeholder_processors_do_not_report_success(string methodName)
    {
        var text = SyncControllerSource();

        var start = text.IndexOf($"private async Task<OpResultDto> {methodName}(", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{methodName} not found in SyncController.");

        // Bound the search at the next processor so we only read this method's body.
        var next = text.IndexOf("private async Task<OpResultDto> Process", start + 10, StringComparison.Ordinal);
        var body = next > start ? text[start..next] : text[start..];

        Assert.DoesNotContain("Success = true", body, StringComparison.Ordinal);
        Assert.Contains("Success = false", body, StringComparison.Ordinal);

        // The old messages claimed work that never happened.
        Assert.DoesNotContain("opened successfully", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("created successfully", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ProcessOpenShift")]
    [InlineData("ProcessCreateCustomer")]
    public void Placeholder_processors_record_a_warning_not_an_information_audit(string methodName)
    {
        var text = SyncControllerSource();
        var start = text.IndexOf($"private async Task<OpResultDto> {methodName}(", StringComparison.Ordinal);
        var next = text.IndexOf("private async Task<OpResultDto> Process", start + 10, StringComparison.Ordinal);
        var body = next > start ? text[start..next] : text[start..];

        // A rejected operation is not routine information.
        Assert.Contains("OperationalAuditSeverity.Warning", body, StringComparison.Ordinal);
    }
}
