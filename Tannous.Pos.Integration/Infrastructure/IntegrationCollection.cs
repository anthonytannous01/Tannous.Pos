namespace Tannous.Pos.Integration.Infrastructure;

/// <summary>Shared integration test collection (one PostgreSQL Testcontainer per collection run).</summary>
public static class IntegrationCollection
{
    public const string Name = "Tannous.Pos.Integration";
}

[CollectionDefinition(IntegrationCollection.Name)]
public sealed class IntegrationCollectionDefinition : ICollectionFixture<IntegrationPostgresFixture>;
