using System.Text.Json;
using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Additive OpenAPI governance: live swagger from the test host must still expose baseline paths and schema root properties.
/// </summary>
public class OpenApiSchemaGovernanceTests : IClassFixture<GovernanceApiFactory>
{
    private readonly GovernanceApiFactory _factory;

    public OpenApiSchemaGovernanceTests(GovernanceApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Live_swagger_retains_baseline_paths_and_schema_properties()
    {
        var baselinePath = Path.Combine(ObservabilitySourceGovernanceTests.RepoRoot(), "governance", "openapi-schema-governance-baseline.json");
        Assert.True(File.Exists(baselinePath), $"Missing baseline {baselinePath}");

        using var baselineDoc = JsonDocument.Parse(await File.ReadAllTextAsync(baselinePath));
        var baseline = baselineDoc.RootElement;

        var client = _factory.CreateClient();
        var swaggerJson = await client.GetStringAsync("/swagger/v1/swagger.json");
        using var spec = JsonDocument.Parse(swaggerJson);
        var root = spec.RootElement;
        Assert.True(root.TryGetProperty("paths", out var paths), "OpenAPI root missing paths.");

        foreach (var pathEntry in baseline.GetProperty("paths").EnumerateArray())
        {
            var template = pathEntry.GetProperty("template").GetString()!;
            var foundKey = paths.EnumerateObject().Select(p => p.Name).FirstOrDefault(p =>
                string.Equals(p, template, StringComparison.OrdinalIgnoreCase));
            Assert.False(string.IsNullOrEmpty(foundKey), $"Missing OpenAPI path (casing drift?): expected `{template}`.");

            var pathNode = paths.GetProperty(foundKey);
            foreach (var m in pathEntry.GetProperty("methods").EnumerateArray())
            {
                var method = m.GetString()!.ToLowerInvariant();
                Assert.True(pathNode.TryGetProperty(method, out _), $"Path `{foundKey}` missing method `{method}`.");
            }
        }

        Assert.True(root.TryGetProperty("components", out var components));
        Assert.True(components.TryGetProperty("schemas", out var schemas), "OpenAPI missing components.schemas.");

        foreach (var schemaEntry in baseline.GetProperty("schemas").EnumerateArray())
        {
            var schemaId = schemaEntry.GetProperty("schemaId").GetString()!;
            Assert.True(schemas.TryGetProperty(schemaId, out var schemaNode),
                $"Missing schema `{schemaId}` (rename drift?). Available: {string.Join(", ", schemas.EnumerateObject().Take(20).Select(p => p.Name))}...");

            Assert.True(schemaNode.TryGetProperty("properties", out var props),
                $"Schema `{schemaId}` missing properties object.");

            foreach (var rp in schemaEntry.GetProperty("requiredRootProperties").EnumerateArray())
            {
                var name = rp.GetString()!;
                Assert.True(props.TryGetProperty(name, out _),
                    $"Schema `{schemaId}` missing property `{name}` (additive-safe: required wire field removed?).");
            }
        }
    }
}
