using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Global Device-Id filter registration (mutation safety baseline).
/// </summary>
public class ProgramMutationGovernanceSourceTests
{
    [Fact]
    public void Program_registers_RequireDeviceIdFilter_for_mutations()
    {
        var path = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "Tannous.Pos.WebApi", "Program.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("RequireDeviceIdFilter", text, StringComparison.Ordinal);
        Assert.Contains("Filters.Add<RequireDeviceIdFilter>", text, StringComparison.Ordinal);
    }
}
