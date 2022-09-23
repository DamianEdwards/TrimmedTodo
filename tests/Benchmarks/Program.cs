using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// Clean up previous run


BenchmarkRunner.Run<HelloWorldWebStartupTimeBenchmarks>();
//BenchmarkRunner.Run<HelloWorldWebStartupTimeBenchmarks>(new DebugInProcessConfig());

[SimpleJob(launchCount: 1, warmupCount: 2, targetCount: 10)]
public class HelloWorldWebStartupTimeBenchmarks
{
    private readonly string _projectName = "HelloWorld.Web";
    private string? _appPath;

    [GlobalSetup(Target = nameof(Default))]
    public void PublishDefault()
    {
        _appPath = ProjectBuilder.Publish(_projectName);
    }

    [Benchmark]
    public void Default()
    {
        AppRunner.Run(_appPath!, ("SHUTDOWN_ON_START", "true"));
    }

    [GlobalSetup(Target = nameof(SelfContained))]
    public void PublishSelfContained()
    {
        _appPath = ProjectBuilder.Publish(_projectName, selfContained: true, trimLevel: TrimLevel.Default);
    }

    [Benchmark]
    public void SelfContained()
    {
        AppRunner.Run(_appPath!, ("SHUTDOWN_ON_START", "true"));
    }

    [GlobalSetup(Target = nameof(Trimmed))]
    public void PublishTrimmed()
    {
        _appPath = ProjectBuilder.Publish(_projectName, trimLevel: TrimLevel.Default);
    }

    [Benchmark]
    public void Trimmed()
    {
        AppRunner.Run(_appPath!, ("SHUTDOWN_ON_START", "true"));
    }

    [GlobalSetup(Target = nameof(AOT))]
    public void PublishAOT()
    {
        _appPath = ProjectBuilder.PublishAot(_projectName);
    }

    [Benchmark]
    public void AOT()
    {
        AppRunner.Run(_appPath!, ("SHUTDOWN_ON_START", "true"));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        var appDir = Path.GetDirectoryName(_appPath);
        if (Directory.Exists(appDir))
        {
            Directory.Delete(appDir, true);
        }
    }
}

class ProjectBuilder
{
    private static readonly string _repoDirPath = PathHelper.GetRepoRoot();
    private static readonly string _projectsDirPath = Path.Combine(_repoDirPath, "src");
    private static readonly string _artifactsDirPath = Path.Combine(_repoDirPath, ".artifacts", "benchmarks");

    public static string Publish(
        string projectName,
        string configuration = "Release",
        string? output = null,
        bool selfContained = false,
        bool singleFile = false,
        TrimLevel trimLevel = TrimLevel.None)
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

        return Publish(projectName, output, args);
    }

    public static string PublishAot(
        string projectName,
        string configuration = "Release",
        string? output = null,
        TrimLevel trimLevel = TrimLevel.Default)
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

        return Publish(projectName, output, args);
    }

    private static string Publish(string projectName, string? output = null, IEnumerable<string>? args = null)
    {
        var projectPath = Path.Combine(_projectsDirPath, projectName, projectName + ".csproj");

        if (!File.Exists(projectPath))
        {
            throw new ArgumentException($"Project at '{projectPath}' could not be found", nameof(projectName));
        }

        var runId = Random.Shared.NextInt64().ToString();
        output ??= Path.Combine(_artifactsDirPath, projectName, runId);

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

        if (!File.Exists(appExePath))
        {
            throw new InvalidOperationException("Could not find application exe");
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
}

class AppRunner
{
    private static readonly string _dotnetFileName = "dotnet" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");

    public static void Run(string appExePath, params (string Name, string Value)[] envVars)
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

        foreach (var envVar in envVars)
        {
            process.StartInfo.Environment.Add(envVar.Name, envVar.Value);
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

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        var _ = process.WaitForExit(_timeout);
    }
}

class PathHelper
{
    private static readonly string _repoDirPath = GetRepoRoot();
    private static readonly string _projectsDirPath = Path.Combine(_repoDirPath, "src");
    private static readonly string _artifactsDirPath = Path.Combine(_repoDirPath, ".artifacts", "benchmarks");

    public static string GetRepoRoot()
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

        if (repoDir is null)
        {
            throw new InvalidOperationException("Couldn't find repo directory");
        }

        return repoDir.FullName;
    }
}

enum TrimLevel
{
    None,
    Default,
    Partial,
    Full
}
