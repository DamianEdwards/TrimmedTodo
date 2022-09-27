using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

if (Directory.Exists(PathHelper.BenchmarkArtifactsDir))
{
    Directory.Delete(PathHelper.BenchmarkArtifactsDir, true);
}

var config = (Debugger.IsAttached ? new DebugInProcessConfig() : DefaultConfig.Instance)
    .AddColumn(new RatioColumn()).HideColumns("Method")
    .WithOrderer(new GroupByProjectNameOrderer())
    .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(42));

if (Debugger.IsAttached)
{
    BenchmarkRunner.Run<StartupTimeBenchmarks>(config
        .WithOption(ConfigOptions.StopOnFirstError, true));
}
else
{
    BenchmarkRunner.Run<StartupTimeBenchmarks>(config
        //.WithOption(ConfigOptions.StopOnFirstError, true)
        );
}

[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 1, invocationCount: 1, targetCount: 10)]
public class StartupTimeBenchmarks
{
    private string? _appPath;

    //[Params("HelloWorld.Console")]
    [Params("HelloWorld.Web")]
    //[Params("HelloWorld.Console", "HelloWorld.Web")]
    //[Params("HelloWorld.Console", "HelloWorld.Web", "TrimmedTodo.Console.EfCore.Sqlite")]
    //[Params("TrimmedTodo.Console.EfCore.Sqlite")]
    public string ProjectName { get; set; } = default!;

    //[ParamsAllValues]
    //[Params(PublishScenario.Default, PublishScenario.Trimmed)]
    [Params(PublishScenario.Default, PublishScenario.Trimmed, PublishScenario.TrimmedReadyToRun, PublishScenario.AOT)]
    //[Params(PublishScenario.SingleFileReadyToRun)]
    //[Params(PublishScenario.Default, PublishScenario.NoAppHost, PublishScenario.ReadyToRun, PublishScenario.SelfContained, PublishScenario.SelfContainedReadyToRun,
    //    PublishScenario.SingleFile, PublishScenario.SingleFileReadyToRun, PublishScenario.Trimmed, PublishScenario.TrimmedReadyToRun)]
    //[Params(PublishScenario.Default, PublishScenario.ReadyToRun, PublishScenario.Trimmed, PublishScenario.AOT)]
    //[Params(PublishScenario.Default, PublishScenario.SingleFile, PublishScenario.ReadyToRun, PublishScenario.Trimmed)]
    //[Params(PublishScenario.TrimmedReadyToRun)]
    public PublishScenario Scenario { get; set; }

    [GlobalSetup]
    public void PublishApp() => _appPath = ProjectBuilder.Publish(ProjectName, Scenario);

    [Benchmark]
    public void StartApp() => AppRunner.Run(_appPath!, ("SHUTDOWN_ON_START", "true"));
}

public enum PublishScenario
{
    Default,
    NoAppHost,
    ReadyToRun,
    SelfContained,
    SelfContainedReadyToRun,
    SingleFile,
    SingleFileReadyToRun,
    Trimmed,
    TrimmedReadyToRun,
    AOT
}

public class RatioColumn : IColumn
{
    public string Id { get; } = "Ratio";
    public string ColumnName { get; } = "Ratio";
    public bool AlwaysShow { get; } = false;
    public ColumnCategory Category { get; } = ColumnCategory.Custom;
    public int PriorityInCategory { get; } = 0;
    public bool IsNumeric { get; } = true;
    public UnitType UnitType { get; } = UnitType.Dimensionless;
    public string Legend { get; } = "[CurrentScenario]/[Default]";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var baseline = summary.BenchmarksCases
            .SingleOrDefault(c =>
            {
                var scenario = c.Parameters[nameof(StartupTimeBenchmarks.Scenario)];

                if (scenario is not PublishScenario.Default)
                {
                    return false;
                }

                for (var i = 0; i < benchmarkCase.Parameters.Count; i++)
                {
                    var p = benchmarkCase.Parameters[i];

                    // Skip scenario parameter
                    if (p.Name == nameof(StartupTimeBenchmarks.Scenario)) continue;

                    if (c.Parameters[p.Name] != p.Value)
                    {
                        // Parameter doesn't match
                        return false;
                    }
                }
                return true;
            });

        if (baseline is not null)
        {
            var baselineReport = summary.Reports.First(r => r.BenchmarkCase == baseline);
            var report = summary.Reports.First(r => r.BenchmarkCase == benchmarkCase);

            return (report.ResultStatistics.Mean / baselineReport.ResultStatistics.Mean).ToString("#.00");
        }

        return "?";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

    public bool IsAvailable(Summary summary) =>
        summary.BenchmarksCases.Where(c => c.Parameters[nameof(StartupTimeBenchmarks.Scenario)] switch
        {
            PublishScenario.Default => true,
            _ => false
        }).Any();

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}

public class GroupByProjectNameOrderer : DefaultOrderer, IOrderer
{
    public override IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
    {
        var benchmarkLogicalGroups = benchmarksCases.GroupBy(b => GetLogicalGroupKeyImpl(b));

        foreach (var logicalGroup in GetLogicalGroupOrder(benchmarkLogicalGroups, benchmarksCases.FirstOrDefault()?.Config.GetLogicalGroupRules()))
        foreach (var benchmark in GetSummaryOrderForGroup(logicalGroup.ToImmutableArray(), summary))
            yield return benchmark;
    }

    public override IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups, IEnumerable<BenchmarkLogicalGroupRule>? order = null)
    {
        var initialOrder = base.GetLogicalGroupOrder(logicalGroups, order);

        var cases = initialOrder.SelectMany(g => g.ToList());
        var newOrder = cases.GroupBy(c => c.Parameters[nameof(StartupTimeBenchmarks.ProjectName)].ToString() ?? "").ToList();
        return newOrder;
    }

    string IOrderer.GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase)
        => GetLogicalGroupKeyImpl(benchmarkCase);

    private static string GetLogicalGroupKeyImpl(BenchmarkCase benchmarkCase)
        => benchmarkCase.Parameters[nameof(StartupTimeBenchmarks.ProjectName)].ToString() ?? "";
}

class ProjectBuilder
{
    public static string Publish(string projectName, PublishScenario scenario) => scenario switch
    {
        PublishScenario.Default => Publish(projectName, runId: Enum.GetName(scenario)),
        PublishScenario.NoAppHost => Publish(projectName, useAppHost: false, runId: Enum.GetName(scenario)),
        PublishScenario.ReadyToRun => Publish(projectName, readyToRun: true, runId: Enum.GetName(scenario)),
        PublishScenario.SelfContained => Publish(projectName, selfContained: true, trimLevel: TrimLevel.None, runId: Enum.GetName(scenario)),
        PublishScenario.SelfContainedReadyToRun => Publish(projectName, selfContained: true, readyToRun: true, trimLevel: TrimLevel.None, runId: Enum.GetName(scenario)),
        PublishScenario.SingleFile => Publish(projectName, selfContained: true, singleFile: true, trimLevel: TrimLevel.None, runId: Enum.GetName(scenario)),
        PublishScenario.SingleFileReadyToRun => Publish(projectName, selfContained: true, singleFile: true, readyToRun: true, trimLevel: TrimLevel.None, runId: Enum.GetName(scenario)),
        PublishScenario.Trimmed => Publish(projectName, selfContained: true, singleFile: true, trimLevel: GetTrimLevel(projectName), runId: Enum.GetName(scenario)),
        PublishScenario.TrimmedReadyToRun => Publish(projectName, selfContained: true, singleFile: true, readyToRun: true, trimLevel: GetTrimLevel(projectName), runId: Enum.GetName(scenario)),
        PublishScenario.AOT => PublishAot(projectName, trimLevel: GetTrimLevel(projectName), runId: Enum.GetName(scenario)),
        _ => throw new ArgumentException("Unrecognized publish scenario", nameof(scenario))
    };

    private static string Publish(
        string projectName,
        string configuration = "Release",
        string? output = null,
        bool selfContained = false,
        bool singleFile = false,
        bool readyToRun = false,
        bool useAppHost = true,
        TrimLevel trimLevel = TrimLevel.None,
        string? runId = null)
    {
        var args = new List<string>
        {
            "--runtime", RuntimeInformation.RuntimeIdentifier,
            selfContained || trimLevel != TrimLevel.None ? "--self-contained" : "--no-self-contained",
            $"/p:PublishTrimmed={(trimLevel == TrimLevel.None ? "false" : "")}",
            $"/p:PublishSingleFile={(singleFile ? "true" : "")}",
            $"/p:PublishReadyToRun={(readyToRun ? "true" : "")}",
            "/p:PublishAot=false"
        };

        if (trimLevel != TrimLevel.None)
        {
            // /p:TrimLevel=
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
            "--runtime", RuntimeInformation.RuntimeIdentifier,
            "--self-contained",
            "/p:PublishSingleFile=",
            "/p:PublishAot=true",
            $"/p:PublishTrimmed={(trimLevel == TrimLevel.None ? "false" : "")}",
        };

        if (trimLevel != TrimLevel.None)
        {
            // /p:TrimLevel=
            args.Add(GetTrimLevelProperty(trimLevel));
        }

        return PublishImpl(projectName, configuration, output, args, runId);
    }

    private static string PublishImpl(string projectName, string configuration = "Release", string? output = null, IEnumerable<string>? args = null, string? runId = null)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(projectName);

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

        cmdArgs.AddRange(new[] { $"--output", output });
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
                UseShellExecute = false,
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
            HandleError(process, "Failed to start application process");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            HandleError(process, $"Application process failed on exit ({process.ExitCode})");
        }

        static void HandleError(Process process, string message)
        {
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            var sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine("Standard output:");
            sb.AppendLine(output);
            sb.AppendLine("Standard error:");
            sb.AppendLine(error);

            throw new InvalidOperationException(sb.ToString());
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

        process.StartInfo.ArgumentList.Add(commandName);
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        var cmdLine = $"{process.StartInfo.FileName} {string.Join(' ', process.StartInfo.ArgumentList)}";
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
