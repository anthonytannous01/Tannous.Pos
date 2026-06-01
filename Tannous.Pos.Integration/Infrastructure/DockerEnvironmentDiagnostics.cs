using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Tannous.Pos.Integration.Infrastructure;

/// <summary>
/// Local Docker/Testcontainers diagnostics for integration tests (no production impact).
/// </summary>
public static class DockerEnvironmentDiagnostics
{
    public const string ObservabilityAvailable = "Integration environment observability: docker available";
    public const string ObservabilityUnavailable = "Integration environment observability: docker unavailable";
    public const string ObservabilityTestcontainerStartup = "Integration environment observability: testcontainer startup";
    public const string ObservabilityPostgresReady = "Integration environment observability: postgres ready";

    private static readonly string[] WindowsDockerPipeCandidates =
    {
        @"\\.\pipe\docker_engine",
        @"\\.\pipe\dockerDesktopLinuxEngine"
    };

    public static DockerEnvironmentReport CollectReport()
    {
        var pipes = WindowsDockerPipeCandidates
            .Select(p => new DockerPipeStatus(p, PipeExists(p)))
            .ToList();

        var dockerCli = TryRunDockerCli("version");
        var dockerInfo = TryRunDockerCli("info --format \"{{.ServerVersion}}\"");

        return new DockerEnvironmentReport(
            OperatingSystemDescription: RuntimeInformation.OSDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            DockerEndpoint: ResolveDockerEndpointHint(pipes),
            DockerPipes: pipes,
            DockerCli: dockerCli,
            DockerInfo: dockerInfo,
            TestcontainersRyukDisabled: Environment.GetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED"),
            SkipWithoutDocker: ShouldSkipWithoutDocker());
    }

    public static void LogReport(DockerEnvironmentReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Integration Docker environment diagnostics ===");
        sb.AppendLine($"OS: {report.OperatingSystemDescription}");
        sb.AppendLine($"Architecture: {report.ProcessArchitecture}");
        sb.AppendLine($"Docker endpoint (hint): {report.DockerEndpoint}");
        foreach (var pipe in report.DockerPipes)
        {
            sb.AppendLine($"  Pipe {pipe.Path}: {(pipe.Exists ? "present" : "missing")}");
        }

        if (report.DockerCli.ExitCode.HasValue)
        {
            sb.AppendLine($"docker version exit code: {report.DockerCli.ExitCode}");
            if (!string.IsNullOrWhiteSpace(report.DockerCli.StdOut))
                sb.AppendLine($"docker version stdout:{Environment.NewLine}{report.DockerCli.StdOut}");
            if (!string.IsNullOrWhiteSpace(report.DockerCli.StdErr))
                sb.AppendLine($"docker version stderr:{Environment.NewLine}{report.DockerCli.StdErr}");
        }
        else
        {
            sb.AppendLine("docker CLI: not executed (docker.exe not found or timed out).");
        }

        sb.AppendLine($"TESTCONTAINERS_RYUK_DISABLED: {report.TestcontainersRyukDisabled ?? "(unset)"}");
        sb.AppendLine($"TANNOUS_INTEGRATION_SKIP_WITHOUT_DOCKER: {report.SkipWithoutDocker}");
        Console.WriteLine(sb.ToString());
    }

    public static async Task<bool> IsDockerAvailableAsync(
        CancellationToken cancellationToken = default,
        int maxAttempts = 3)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Require daemon reachability (client-only `docker version` is not sufficient on Windows).
            var info = await TryRunDockerCliAsync("info --format \"{{.ServerVersion}}\"", TimeSpan.FromSeconds(15))
                .ConfigureAwait(false);
            if (info.ExitCode == 0 && !string.IsNullOrWhiteSpace(info.StdOut))
                return true;

            if (attempt < maxAttempts)
            {
                Console.WriteLine(
                    $"Integration environment observability: docker probe attempt {attempt}/{maxAttempts} failed; retrying.");
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        return false;
    }

    public static async Task EnsureDockerAvailableAsync(CancellationToken cancellationToken = default)
    {
        var report = CollectReport();
        LogReport(report);

        if (await IsDockerAvailableAsync(cancellationToken))
        {
            Console.WriteLine(ObservabilityAvailable);
            return;
        }

        Console.WriteLine(ObservabilityUnavailable);

        var summary =
            $"Docker engine not reachable. Endpoint hint: {report.DockerEndpoint}. " +
            $"Pipes: {string.Join(", ", report.DockerPipes.Select(p => $"{p.Path}={(p.Exists ? "ok" : "missing")}"))}.";

        throw new IntegrationDockerUnavailableException(summary);
    }

    public static bool ShouldSkipWithoutDocker() =>
        string.Equals(
            Environment.GetEnvironmentVariable("TANNOUS_INTEGRATION_SKIP_WITHOUT_DOCKER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static string ResolveDockerEndpointHint(IReadOnlyList<DockerPipeStatus> pipes)
    {
        if (pipes.Any(p => p.Path.EndsWith("dockerDesktopLinuxEngine", StringComparison.OrdinalIgnoreCase) && p.Exists))
            return "npipe://./pipe/dockerDesktopLinuxEngine";

        if (pipes.Any(p => p.Path.EndsWith("docker_engine", StringComparison.OrdinalIgnoreCase) && p.Exists))
            return "npipe://./pipe/docker_engine";

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "npipe://./pipe/dockerDesktopLinuxEngine (expected when Docker Desktop WSL2 backend is running)"
            : "unix:///var/run/docker.sock (typical Linux/macOS)";
    }

    private static bool PipeExists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    private static void AppendCliResult(StringBuilder sb, string label, DockerCliResult result)
    {
        if (result.ExitCode.HasValue)
        {
            sb.AppendLine($"{label} exit code: {result.ExitCode}");
            if (!string.IsNullOrWhiteSpace(result.StdOut))
                sb.AppendLine($"{label} stdout:{Environment.NewLine}{result.StdOut}");
            if (!string.IsNullOrWhiteSpace(result.StdErr))
                sb.AppendLine($"{label} stderr:{Environment.NewLine}{result.StdErr}");
        }
        else
        {
            sb.AppendLine($"{label}: not executed ({result.StartError ?? "docker.exe not found or timed out"}).");
        }
    }

    private static DockerCliResult TryRunDockerCli(string arguments, TimeSpan? timeout = null) =>
        TryRunDockerCliAsync(arguments, timeout).GetAwaiter().GetResult();

    private static async Task<DockerCliResult> TryRunDockerCliAsync(string arguments, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(20);
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!process.Start())
                return new DockerCliResult(null, string.Empty, "Failed to start docker.exe", null);

            using var cts = new CancellationTokenSource(timeout.Value);
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

            try
            {
                if (!process.WaitForExit((int)timeout.Value.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // best effort
                    }

                    return new DockerCliResult(null, string.Empty, $"docker {arguments} timed out", null);
                }
            }
            catch
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // best effort
                }

                return new DockerCliResult(null, string.Empty, $"docker {arguments} timed out", null);
            }

            string stdout;
            string stderr;
            try
            {
                stdout = await stdoutTask.ConfigureAwait(false);
                stderr = await stderrTask.ConfigureAwait(false);
            }
            catch
            {
                stdout = string.Empty;
                stderr = string.Empty;
            }

            return new DockerCliResult(process.ExitCode, stdout, stderr, null);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            return new DockerCliResult(null, string.Empty, string.Empty, ex.Message);
        }
    }
}

public sealed record DockerEnvironmentReport(
    string OperatingSystemDescription,
    string ProcessArchitecture,
    string DockerEndpoint,
    IReadOnlyList<DockerPipeStatus> DockerPipes,
    DockerCliResult DockerCli,
    DockerCliResult DockerInfo,
    string? TestcontainersRyukDisabled,
    bool SkipWithoutDocker);

public sealed record DockerPipeStatus(string Path, bool Exists);

public sealed record DockerCliResult(int? ExitCode, string StdOut, string StdErr, string? StartError);
