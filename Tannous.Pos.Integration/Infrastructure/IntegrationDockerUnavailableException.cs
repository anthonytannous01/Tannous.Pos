namespace Tannous.Pos.Integration.Infrastructure;

/// <summary>
/// Raised when integration tests require Docker/Testcontainers but the engine is not reachable.
/// </summary>
public sealed class IntegrationDockerUnavailableException : Exception
{
    public const string Remediation =
        """
        Docker is not reachable for integration tests.

        Remediation (Windows + Docker Desktop):
        1. Start Docker Desktop and wait until it shows "Docker Desktop is running".
        2. Ensure "Use the WSL 2 based engine" is enabled (Settings → General).
        3. Ensure Linux containers are active (default for desktop-linux context).
        4. Verify in PowerShell:
             docker version
             docker ps
        5. Confirm a pipe exists:
             Test-Path \\.\pipe\dockerDesktopLinuxEngine
             Test-Path \\.\pipe\docker_engine
        6. Re-run:
             dotnet test tests\Tannous.Pos.Integration\Tannous.Pos.Integration.csproj -c Release

        Optional: set TANNOUS_INTEGRATION_SKIP_WITHOUT_DOCKER=true to skip (not fail) when Docker is down.
        """;

    public IntegrationDockerUnavailableException(string message, Exception? inner = null)
        : base(message + Environment.NewLine + Remediation, inner)
    {
    }
}
