using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5287");
builder.Services.AddSingleton<TurkeySuperLigDataRefreshStatus>();

const string StatusPath = "/api/data-refresh/turkey-super-lig-2026-27/status";
var app = builder.Build();

app.MapGet("/", () => Results.Redirect(StatusPath));
app.MapGet(
    StatusPath,
    async (TurkeySuperLigDataRefreshStatus status, CancellationToken cancellationToken) =>
        Results.Text(
            await status.GetAsync(cancellationToken),
            contentType: "application/json; charset=utf-8"));

app.Run();

internal sealed class TurkeySuperLigDataRefreshStatus(IConfiguration configuration, IWebHostEnvironment environment)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedJson;
    private DateTimeOffset _cacheExpiresAt;

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (_cachedJson is not null && DateTimeOffset.UtcNow < _cacheExpiresAt)
        {
            return _cachedJson;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cachedJson is not null && DateTimeOffset.UtcNow < _cacheExpiresAt)
            {
                return _cachedJson;
            }

            var workspaceRoot = configuration["DataRefresh:WorkspaceRoot"]
                ?? FindWorkspaceRoot(environment.ContentRootPath);
            var scriptPath = Path.Combine(workspaceRoot, "tools", "Update-TurkeySuperLig202627DataPack.ps1");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Süper Lig data refresh script was not found.", scriptPath);
            }

            using var process = new Process
            {
                StartInfo = CreateStartInfo(scriptPath, workspaceRoot),
            };
            process.Start();

            var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await standardOutput;
            var error = await standardError;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Live roster check failed with exit code {process.ExitCode}: {error.Trim()}");
            }

            _cachedJson = output.Trim();
            _cacheExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
            return _cachedJson;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static ProcessStartInfo CreateStartInfo(string scriptPath, string workspaceRoot)
    {
        var startInfo = new ProcessStartInfo(ResolvePowerShellExecutable())
        {
            WorkingDirectory = workspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-WorkspaceRoot");
        startInfo.ArgumentList.Add(workspaceRoot);
        startInfo.ArgumentList.Add("-SkipAssets");
        startInfo.ArgumentList.Add("-CheckOnly");
        startInfo.ArgumentList.Add("-AsJson");
        return startInfo;
    }

    private static string ResolvePowerShellExecutable()
    {
        foreach (var name in new[] { "pwsh.exe", "powershell.exe" })
        {
            var resolved = FindOnPath(name);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        var windowsPowerShell = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");
        if (File.Exists(windowsPowerShell))
        {
            return windowsPowerShell;
        }

        throw new FileNotFoundException(
            "PowerShell bulunamadı. Windows PowerShell veya PowerShell 7 (pwsh) gerekir.");
    }

    private static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string FindWorkspaceRoot(string startPath)
    {
        for (var directory = new DirectoryInfo(startPath); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FootballCareerSimulator.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Workspace root was not found. Configure DataRefresh:WorkspaceRoot explicitly.");
    }
}
