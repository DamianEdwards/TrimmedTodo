using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

if (Directory.Exists(PathHelper.BenchmarkArtifactsDir))
{
    Directory.Delete(PathHelper.BenchmarkArtifactsDir, true);
}

if (Debugger.IsAttached)
{
    BenchmarkRunner.Run<StartupTimeBenchmarks>(new DebugInProcessConfig());
}
else
{
    BenchmarkRunner.Run<StartupTimeBenchmarks>();
}

[SimpleJob(launchCount: 1, warmupCount: 2, targetCount: 10)]
public class StartupTimeBenchmarks
{
    private string? _appPath;

    [Params("HelloWorld.Web")]
    public string ProjectName { get; set; } = default!;

    [ParamsAllValues]
    public PublishScenario Scenario { get; set; }

    [GlobalSetup]
    public void PublishApp()
    {
        _appPath = ProjectBuilder.Publish(ProjectName, Scenario);
    }

    [Benchmark]
    public void StartApp()
    {
        AppRunner.Run(_appPath!, ("SHUTDOWN_ON_START", "true"));
    }
}

public enum PublishScenario
{
    Default,
    SelfContained,
    Trimmed,
    AOT
}

class ProjectBuilder
{
    public static string Publish(string projectName, PublishScenario scenario) => scenario switch
    {
        PublishScenario.Default => Publish(projectName, runId: Enum.GetName(scenario)),
        PublishScenario.SelfContained => Publish(projectName, selfContained: true, trimLevel: TrimLevel.None, runId: Enum.GetName(scenario)),
        PublishScenario.Trimmed => Publish(projectName, selfContained: true, trimLevel: TrimLevel.Default, runId: Enum.GetName(scenario)),
        PublishScenario.AOT => PublishAot(projectName, runId: Enum.GetName(scenario)),
        _ => throw new ArgumentException("Unrecognized publish scenario", nameof(scenario))
    };

    private static string Publish(
        string projectName,
        string configuration = "Release",
        string? output = null,
        bool selfContained = false,
        bool singleFile = false,
        TrimLevel trimLevel = TrimLevel.None,
        string? runId = null)
    {
        var args = new List<string>
        {
            "--configuration", configuration,
            "--self-contained", trimLevel != TrimLevel.None ? "true" : selfContained.ToString().ToLowerInvariant(),
            "--runtime", RuntimeInformation.RuntimeIdentifier,
            $"/p:PublishTrimmed={(trimLevel == TrimLevel.None ? "false" : "true")}",
            $"/p:PublishSingleFile={singleFile.ToString().ToLowerInvariant()}",
            "/p:PublishAot=false"
        };

        if (trimLevel != TrimLevel.None)
        {
            args.Add(GetTrimLevelProperty(trimLevel));
        }

        return PublishImpl(projectName, output, args, runId);
    }

    private static string PublishAot(
        string projectName,
        string configuration = "Release",
        string? output = null,
        TrimLevel trimLevel = TrimLevel.Default,
        string? runId = null)
    {
        var args = new List<string>
        {
            "--configuration", configuration,
            "--self-contained", "true",
            "--runtime", RuntimeInformation.RuntimeIdentifier,
            "/p:PublishAot=true",
            $"/p:PublishTrimmed={(trimLevel == TrimLevel.None ? "false" : "true")}",
            "/p:PublishSingleFile=false"
        };

        if (trimLevel != TrimLevel.None)
        {
            args.Add(GetTrimLevelProperty(trimLevel));
        }

        return PublishImpl(projectName, output, args, runId);
    }

    private static string PublishImpl(string projectName, string? output = null, IEnumerable<string>? args = null, string? runId = null)
    {
        var projectPath = Path.Combine(PathHelper.ProjectsDir, projectName, projectName + ".csproj");

        if (!File.Exists(projectPath))
        {
            throw new ArgumentException($"Project at '{projectPath}' could not be found", nameof(projectName));
        }

        runId ??= Random.Shared.NextInt64().ToString();
        output ??= Path.Combine(PathHelper.BenchmarkArtifactsDir, projectName, runId);

        var publishArgs = new List<string>
        {
            projectPath,
            "--output", output
        };
        if (args is not null)
        {
            publishArgs.AddRange(args);
        }

        //Directory.Delete(output, true);

        DotNetCli.Publish(publishArgs);

        var appExePath = Path.Join(output, projectName);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            appExePath += ".exe";
        }

        return !File.Exists(appExePath) ? throw new InvalidOperationException("Could not find application exe") : appExePath;
    }

    private static string GetTrimLevelProperty(TrimLevel trimLevel)
    {
        return "/p:TrimMode=" + trimLevel switch
        {
            TrimLevel.Default => "",
            _ => Enum.GetName(trimLevel)?.ToLower() ?? ""
        };
    }
}

class AppRunner
{
    private static readonly string _dotnetFileName = "dotnet" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");

    public static void Run(string appExePath, params (string, string)[] envVars)
    {
        if (!File.Exists(appExePath))
        {
            throw new ArgumentException("Could not find application exe", nameof(appExePath));
        }

        var isAppHost = !Path.GetExtension(appExePath)!.Equals(".dll", StringComparison.OrdinalIgnoreCase);

        var process = new Process
        {
            StartInfo =
            {
                FileName = isAppHost ? appExePath : _dotnetFileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        if (!isAppHost)
        {
            process.StartInfo.ArgumentList.Add(appExePath);
        }

        foreach (var (name, value) in envVars)
        {
            process.StartInfo.Environment.Add(name, value);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start application process");
        }

        process.WaitForExit();
    }
}


class DotNetCli
{
    private static readonly string _fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    public static void Publish(IEnumerable<string> args)
    {
        var process = new Process
        {
            StartInfo =
            {
                FileName = _fileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.StartInfo.ArgumentList.Add("publish");
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("dotnet publish failed");
        }

        //var output = process.StandardOutput.ReadToEnd();
        //var error = process.StandardError.ReadToEnd();

        var _ = process.WaitForExit(_timeout);
    }
}

class PathHelper
{
    public static string RepoRoot { get; } = GetRepoRoot();
    public static string ProjectsDir { get; } = Path.Combine(RepoRoot, "src");
    public static string ArtifactsDir { get; } = Path.Combine(RepoRoot, ".artifacts");
    public static string BenchmarkArtifactsDir { get; } = Path.Combine(ArtifactsDir, "benchmarks");

    private static string GetRepoRoot()
    {
        var currentDirPath = Environment.CurrentDirectory;
        DirectoryInfo? repoDir = null;
        while (true)
        {
            var parent = Directory.GetParent(currentDirPath);

            if (parent is null)
            {
                // We hit the root
                break;
            }

            if (File.Exists(Path.Join(parent.FullName, "TrimmedTodo.sln")))
            {
                // We're in the repo root
                repoDir = parent;
                break;
            }

            currentDirPath = parent.FullName;
        }

        return repoDir is null ? throw new InvalidOperationException("Couldn't find repo directory") : repoDir.FullName;
    }
}

enum TrimLevel
{
    None,
    Default,
    Partial,
    Full
}
