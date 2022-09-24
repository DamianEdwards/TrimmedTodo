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

    //[Params("HelloWorld.Web", "HelloWorld.Console")]
    [Params("TrimmedTodo.Console.EfCore.Sqlite")]
    public string ProjectName { get; set; } = default!;

    //[ParamsAllValues]
    //[Params(PublishScenario.Default, PublishScenario.Trimmed, PublishScenario.AOT)]
    [Params(PublishScenario.Default, PublishScenario.SingleFile, PublishScenario.Trimmed)]
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
    NoAppHost,
    SelfContained,
    SingleFile,
    Trimmed,
    AOT
}

class ProjectBuilder
{
    public static string Publish(string projectName, PublishScenario scenario) => scenario switch
    {
        PublishScenario.Default => Publish(projectName, runId: Enum.GetName(scenario)),
        PublishScenario.NoAppHost => Publish(projectName, useAppHost: false, runId: Enum.GetName(scenario)),
        PublishScenario.SelfContained => Publish(projectName, selfContained: true, trimLevel: TrimLevel.None, runId: Enum.GetName(scenario)),
        PublishScenario.SingleFile => Publish(projectName, selfContained: true, singleFile: true, trimLevel: TrimLevel.None, runId: Enum.GetName(scenario)),
        PublishScenario.Trimmed => Publish(projectName, selfContained: true, singleFile: true, trimLevel: GetTrimLevel(projectName), runId: Enum.GetName(scenario)),
        PublishScenario.AOT => PublishAot(projectName, trimLevel: GetTrimLevel(projectName), runId: Enum.GetName(scenario)),
        _ => throw new ArgumentException("Unrecognized publish scenario", nameof(scenario))
    };

    private static string Publish(
        string projectName,
        string configuration = "Release",
        string? output = null,
        bool selfContained = false,
        bool singleFile = false,
        bool useAppHost = true,
        TrimLevel trimLevel = TrimLevel.None,
        string? runId = null)
    {
        var args = new List<string>
        {
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

        if (!useAppHost)
        {
            args.Add("/p:UseAppHost=false");
        }

        return PublishImpl(projectName, configuration, output, args, runId);
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

        return PublishImpl(projectName, configuration, output, args, runId);
    }

    private static string PublishImpl(string projectName, string configuration = "Release", string? output = null, IEnumerable<string>? args = null, string? runId = null)
    {
        var projectPath = Path.Combine(PathHelper.ProjectsDir, projectName, projectName + ".csproj");

        if (!File.Exists(projectPath))
        {
            throw new ArgumentException($"Project at '{projectPath}' could not be found", nameof(projectName));
        }

        runId ??= Random.Shared.NextInt64().ToString();
        output ??= Path.Combine(PathHelper.BenchmarkArtifactsDir, projectName, runId);

        var cmdArgs = new List<string>
        {
            projectPath,
            $"--configuration", configuration
        };

        DotNetCli.Clean(cmdArgs);

        cmdArgs.AddRange(new[] { $"--output", output});
        cmdArgs.Add("--disable-build-servers");
        if (args is not null)
        {
            cmdArgs.AddRange(args);
        }

        DotNetCli.Publish(cmdArgs);

        var appExePath = Path.Join(output, projectName);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            appExePath += ".exe";
        }

        if (!File.Exists(appExePath))
        {
            var appDllPath = Path.Join(output, projectName + ".dll");
            return !File.Exists(appDllPath) ? throw new InvalidOperationException($"Could not find application exe or dll '{appDllPath}'") : appDllPath;
        }

        return appExePath;
    }

    private static string GetTrimLevelProperty(TrimLevel trimLevel)
    {
        return "/p:TrimMode=" + trimLevel switch
        {
            TrimLevel.Default => "",
            _ => Enum.GetName(trimLevel)?.ToLower() ?? ""
        };
    }

    private static TrimLevel GetTrimLevel(string projectName)
    {
        if (projectName.Contains("EfCore", StringComparison.OrdinalIgnoreCase)
            || projectName.Contains("Dapper", StringComparison.OrdinalIgnoreCase))
        {
            return TrimLevel.Partial;
        }

        if (projectName.Contains("Console"))
        {
            return TrimLevel.Default;
        }

        if (projectName.Contains("HelloWorld"))
        {
            return TrimLevel.Full;
        }

        return TrimLevel.Default;
    }
}

class AppRunner
{
    private static readonly string _dotnetFileName = "dotnet" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");

    public static void Run(string appExePath, params (string, string)[] envVars)
    {
        if (!File.Exists(appExePath))
        {
            throw new ArgumentException($"Could not find application exe '{appExePath}'", nameof(appExePath));
        }

        var isAppHost = !Path.GetExtension(appExePath)!.Equals(".dll", StringComparison.OrdinalIgnoreCase);

        var process = new Process
        {
            StartInfo =
            {
                FileName = isAppHost ? appExePath : _dotnetFileName,
                UseShellExecute = false
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

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Application process failed on exit ({process.ExitCode})");
        }
    }
}


class DotNetCli
{
    private static readonly string _fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    public static void Clean(IEnumerable<string> args)
    {
        RunCommand("clean", args);
    }

    public static void Publish(IEnumerable<string> args)
    {
        RunCommand("publish", args);
    }

    private static void RunCommand(string commandName, IEnumerable<string> args)
    {
        var process = new Process
        {
            StartInfo =
            {
                FileName = _fileName,
                UseShellExecute = false
            }
        };

        process.StartInfo.Arguments = $"{commandName} {string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";

        var cmdLine = $"{process.StartInfo.FileName} {process.StartInfo.Arguments}";
        Console.WriteLine("Running dotnet CLI with cmd line:");
        Console.WriteLine(cmdLine);
        Console.WriteLine();

        if (!process.Start())
        {
            throw new InvalidOperationException($"dotnet {commandName} failed");
        }

        process.WaitForExit();

        if (!process.WaitForExit(_timeout))
        {
            process.Kill();

            throw new InvalidOperationException($"dotnet {commandName} took longer than the allowed time of {_timeout}");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet {commandName} failed on exit ({process.ExitCode})");
        }
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
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        DirectoryInfo? repoDir = null;

        while (true)
        {
            if (currentDir is null)
            {
                // We hit the file system root
                break;
            }

            if (File.Exists(Path.Join(currentDir.FullName, "TrimmedTodo.sln")))
            {
                // We're in the repo root
                repoDir = currentDir;
                break;
            }

            currentDir = currentDir.Parent;
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
