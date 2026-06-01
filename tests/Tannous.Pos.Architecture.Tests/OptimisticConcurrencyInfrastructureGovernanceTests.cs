using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Anchors RowVersion optimistic concurrency on hot aggregates (model + migration visibility).
/// </summary>
public class OptimisticConcurrencyInfrastructureGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    [Fact]
    public void PosDbContextModelSnapshot_orders_inventory_shifts_have_rowversion_concurrency()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "PosDbContextModelSnapshot.cs");
        var text = File.ReadAllText(path);

        var matches = System.Text.RegularExpressions.Regex.Matches(
            text,
            @"b\.Property<byte\[\]>\(""RowVersion""\)");
        Assert.True(matches.Count >= 3, $"Expected at least three RowVersion concurrency properties in snapshot, found {matches.Count}.");
    }

    [Fact]
    public void AddRowVersion_migration_adds_columns_to_three_tables()
    {
        var path = Path.Combine(RepoRoot(), "Tannous.Pos.Infrastructure", "Migrations", "20260513121500_AddRowVersionConcurrencyToOrdersInventoryItemsShifts.cs");
        Assert.True(File.Exists(path), $"Missing {path}");
        var text = File.ReadAllText(path);
        Assert.Contains("Orders", text, StringComparison.Ordinal);
        Assert.Contains("InventoryItems", text, StringComparison.Ordinal);
        Assert.Contains("Shifts", text, StringComparison.Ordinal);
        Assert.Contains("RowVersion", text, StringComparison.Ordinal);
    }
}
