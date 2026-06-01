using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration.Infrastructure;

/// <summary>
/// One PostgreSQL Testcontainer per integration test collection; per-test-class isolated databases.
/// </summary>
public sealed class IntegrationPostgresFixture : IAsyncLifetime
{
    private const int ContainerStartMaxAttempts = 3;
    private const int PostgresReadinessMaxAttempts = 12;

    private static readonly SemaphoreSlim SchemaMigrationLock = new(1, 1);
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private PostgreSqlContainer? _postgres;
    private bool _postgresStarted;

    /// <summary>Set when <c>TANNOUS_INTEGRATION_SKIP_WITHOUT_DOCKER=true</c> and Docker is unavailable.</summary>
    public string? SkipReason { get; private set; }

    public PostgreSqlContainer Postgres =>
        _postgres ?? throw new InvalidOperationException(
            "PostgreSQL testcontainer is not initialized. Call EnsureInitializedAsync first.");

    public async Task InitializeAsync() => await EnsureInitializedAsync();

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_postgresStarted || SkipReason != null)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_postgresStarted || SkipReason != null)
                return;

            var report = DockerEnvironmentDiagnostics.CollectReport();
            DockerEnvironmentDiagnostics.LogReport(report);

            if (!await DockerEnvironmentDiagnostics.IsDockerAvailableAsync(cancellationToken))
            {
                Console.WriteLine(DockerEnvironmentDiagnostics.ObservabilityUnavailable);
                var summary =
                    $"Docker engine not reachable after {ContainerStartMaxAttempts} probe attempt(s). " +
                    $"Endpoint hint: {report.DockerEndpoint}. " +
                    $"Pipes: {string.Join(", ", report.DockerPipes.Select(p => $"{p.Path}={(p.Exists ? "ok" : "missing")}"))}.";

                if (DockerEnvironmentDiagnostics.ShouldSkipWithoutDocker())
                {
                    SkipReason = summary + Environment.NewLine + IntegrationDockerUnavailableException.Remediation;
                    Console.WriteLine(
                        "Integration environment observability: integration tests skipped (TANNOUS_INTEGRATION_SKIP_WITHOUT_DOCKER=true).");
                    return;
                }

                throw new IntegrationDockerUnavailableException(summary);
            }

            Console.WriteLine(DockerEnvironmentDiagnostics.ObservabilityAvailable);
            await StartPostgresContainerWithRetryAsync(cancellationToken);

            _postgresStarted = true;
            Console.WriteLine(DockerEnvironmentDiagnostics.ObservabilityPostgresReady);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> AllocateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (SkipReason != null)
            return string.Empty;

        var databaseName = "it_" + Guid.NewGuid().ToString("N")[..12];
        var adminConnectionString = GetAdminConnectionString();

        await using (var connection = new NpgsqlConnection(adminConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName
        };
        return ApplyConnectionPoolSettings(builder.ConnectionString);
    }

    public async Task DropDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || SkipReason != null)
            return;

        var target = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = target.Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            string.Equals(databaseName, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var adminConnectionString = GetAdminConnectionString();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @db AND pid <> pg_backend_pid();
                """;
            terminate.Parameters.AddWithValue("db", databaseName);
            await terminate.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
        await drop.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task EnsureDatabaseSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var pooledConnectionString = ApplyConnectionPoolSettings(connectionString);
        await SchemaMigrationLock.WaitAsync(cancellationToken);
        try
        {
            var options = new DbContextOptionsBuilder<PosDbContext>()
                .UseNpgsql(
                    pooledConnectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(PosDbContext).Assembly.FullName))
                .Options;

            await using var context = new PosDbContext(options);
            await context.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            SchemaMigrationLock.Release();
        }
    }

    public WebApplicationFactory<Program> CreateWebApplicationFactory(string connectionString) =>
        new TannousPosIntegrationWebApplicationFactory(ApplyConnectionPoolSettings(connectionString));

    internal static string ApplyConnectionPoolSettings(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = 20,
            Timeout = 30
        };
        return builder.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_postgresStarted && _postgres != null)
        {
            try
            {
                await _postgres.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Integration environment observability: postgres container disposal failed (non-fatal). {ex.Message}");
            }

            _postgresStarted = false;
            _postgres = null;
        }
    }

    private async Task StartPostgresContainerWithRetryAsync(CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= ContainerStartMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_postgres != null)
            {
                await SafeDisposeContainerAsync(_postgres);
                _postgres = null;
            }

            _postgres = CreatePostgresContainer();
            Console.WriteLine(
                $"Integration environment observability: testcontainer startup attempt {attempt}/{ContainerStartMaxAttempts}");
            Console.WriteLine(DockerEnvironmentDiagnostics.ObservabilityTestcontainerStartup);

            try
            {
                await _postgres.StartAsync(cancellationToken);
                await WaitForPostgresReadinessAsync(cancellationToken);
                return;
            }
            catch (DockerUnavailableException ex)
            {
                lastException = new IntegrationDockerUnavailableException(
                    "Testcontainers could not reach the Docker engine while starting PostgreSQL.",
                    ex);
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine(
                    $"Integration environment observability: postgres startup attempt {attempt} failed. {ex.GetType().Name}: {ex.Message}");
            }

            if (attempt < ContainerStartMaxAttempts)
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
        }

        throw new InvalidOperationException(
            $"PostgreSQL Testcontainer failed to start after {ContainerStartMaxAttempts} attempt(s).",
            lastException);
    }

    private static PostgreSqlContainer CreatePostgresContainer() =>
        new PostgreSqlBuilder("postgres:15")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("password")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
            .Build();

    private async Task WaitForPostgresReadinessAsync(CancellationToken cancellationToken)
    {
        var connectionString = Postgres.GetConnectionString();
        Exception? lastException = null;

        for (var attempt = 1; attempt <= PostgresReadinessMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandTimeout = 5;
                var result = await command.ExecuteScalarAsync(cancellationToken);
                if (result is not null)
                    return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine(
                    $"Integration environment observability: postgres readiness probe {attempt}/{PostgresReadinessMaxAttempts} failed. {ex.Message}");
            }

            if (attempt < PostgresReadinessMaxAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
        }

        throw new InvalidOperationException(
            "PostgreSQL container started but did not accept SQL connections within the readiness window.",
            lastException);
    }

    private static async Task SafeDisposeContainerAsync(PostgreSqlContainer container)
    {
        try
        {
            await container.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Integration environment observability: failed to dispose prior postgres container (non-fatal). {ex.Message}");
        }
    }

    private string GetAdminConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(Postgres.GetConnectionString())
        {
            Database = "postgres"
        };
        return builder.ConnectionString;
    }
}
