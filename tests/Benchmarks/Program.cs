using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Benchmarks;

if (Directory.Exists(PathHelper.BenchmarkArtifactsDir))
{
    Directory.Delete(PathHelper.BenchmarkArtifactsDir, true);
}

var job = Job.Default
        .WithToolchain(InProcessEmitToolchain.Instance)
        .WithStrategy(RunStrategy.ColdStart)
        .WithIterationCount(10);

if (Debugger.IsAttached)
{
    job = job.WithCustomBuildConfiguration("Debug");
}

var config = DefaultConfig.Instance
    .AddJob(job)
    .AddColumnProvider(new AppMetricsColumnProvider())
    //.AddColumn(new ParameterRatioColumn(nameof(StartupTimeBenchmarks.PublishKind), PublishScenario.Default))
    //.AddColumn(new ParameterRatioColumn(nameof(StartupTimeBenchmarks.Project), BaselineValueComparisonKind.StartsWith))
    .HideColumns("Method")
    .WithOrderer(new GroupByProjectNameOrderer())
    .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(42));

if (Debugger.IsAttached)
{
    config = config.WithOptions(ConfigOptions.KeepBenchmarkFiles | ConfigOptions.DisableOptimizationsValidator);
}

BenchmarkRunner.Run<StartupTimeBenchmarks>(config);

public class StartupTimeBenchmarks
{
    private ProjectBuilder _projectBuilder = default!;

    public static IEnumerable<string> ProjectNames() => new[]
    {
        //"HelloWorld.Console",
        "HelloWorld.Web",
        "HelloWorld.Web.Stripped",
        "HelloWorld.HttpListener",
        //"TrimmedTodo.Console.EfCore.Sqlite",
        //"TrimmedTodo.MinimalApi.Sqlite",
        //"TrimmedTodo.MinimalApi.Dapper.Sqlite",
        //"TrimmedTodo.MinimalApi.EfCore.Sqlite",
        //"TrimmedTodo.WebApi.EfCore.Sqlite",
    };

    public static IEnumerable<PublishScenario> Scenarios() => new[]
    {
        //PublishScenario.Default,
        //PublishScenario.NoAppHost,
        //PublishScenario.ReadyToRun,
        //PublishScenario.SelfContained,
        //PublishScenario.SelfContainedReadyToRun,
        //PublishScenario.SingleFile,
        //PublishScenario.SingleFileReadyToRun,
        //PublishScenario.Trimmed,
        //PublishScenario.TrimmedReadyToRun,
        PublishScenario.AOT
    };

    [ParamsSource(nameof(ProjectNames))]
    public string Project { get; set; } = default!;

    [ParamsSource(nameof(Scenarios))]
    public PublishScenario PublishKind { get; set; }

    [GlobalSetup]
    public void CreateAndPublishApp()
    {
        _projectBuilder = new ProjectBuilder(Project, PublishKind);
        _projectBuilder.Publish();
    }

    [Benchmark]
    public void RunApp() => _projectBuilder.Run();

    [GlobalCleanup]
    public void SaveAppOutput()
    {
        _projectBuilder.SaveOutput();
    }
}
