using System.Diagnostics;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarks;

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
        //.WithOption(ConfigOptions.StopOnFirstError, true)
        );
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
    private string _appPath = default!;
    private string? _userSecretsId = default;

    public static IEnumerable<string> ProjectNames() => new[]
    {
        //"HelloWorld.Console",
        //"HelloWorld.Web",
        //"HelloWorld.Web.Stripped",
        //"TrimmedTodo.Console.EfCore.Sqlite",
        "TrimmedTodo.MinimalApi.Sqlite",
        "TrimmedTodo.MinimalApi.Dapper.Sqlite",
        "TrimmedTodo.MinimalApi.EfCore.Sqlite",
        //"TrimmedTodo.WebApi.EfCore.Sqlite",
    };

    public static IEnumerable<PublishScenario> Scenarios() => new[]
    {
        PublishScenario.Default,
        //PublishScenario.NoAppHost,
        //PublishScenario.ReadyToRun,
        //PublishScenario.SelfContained,
        //PublishScenario.SelfContainedReadyToRun,
        //PublishScenario.SingleFile,
        //PublishScenario.SingleFileReadyToRun,
        //PublishScenario.Trimmed,
        PublishScenario.TrimmedReadyToRun,
        //PublishScenario.AOT
    };

    [ParamsSource(nameof(ProjectNames))]
    public string ProjectName { get; set; } = default!;

    [ParamsSource(nameof(Scenarios))]
    public PublishScenario Scenario { get; set; }

    [GlobalSetup]
    public void PublishApp()
    {
        var (appPath, userSecretsId) = ProjectBuilder.Publish(ProjectName, Scenario);
        _appPath = appPath;
        _userSecretsId = userSecretsId;
    }

    [Benchmark]
    public void StartApp() => AppRunner.Run(_appPath, GetEnvVars());

    private (string, string)[] GetEnvVars()
    {
        var result = new List<(string, string)> { ("SHUTDOWN_ON_START", "true") };

        if (_userSecretsId is not null)
        {
            // Set env var for JWT signing key
            var userSecretsJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "UserSecrets", _userSecretsId, "secrets.json");

            if (!File.Exists(userSecretsJsonPath))
            {
                throw new InvalidOperationException($"Could not find user secrets json file at path '{userSecretsJsonPath}'. " +
                    "Project has a UserSecretsId but has not been initialized for JWT authentication." +
                    "Please run 'dotnet user-jwts create' in the '$projectName' directory.");
            }

            var userSecretsJson = JsonDocument.Parse(File.OpenRead(userSecretsJsonPath));
            var configKeyName = "Authentication:Schemes:Bearer:SigningKeys";
            var jwtSigningKey = userSecretsJson.RootElement.GetProperty(configKeyName).EnumerateArray()
                .Single(o => o.GetProperty("Issuer").GetString() == "dotnet-user-jwts")
                .GetProperty("Value").GetString();

            if (jwtSigningKey is not null)
            {
                result.Add(("JWT_SIGNING_KEY", jwtSigningKey));
            }
        }

        return result.ToArray();
    }
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
