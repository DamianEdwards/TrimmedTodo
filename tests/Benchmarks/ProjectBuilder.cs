using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Benchmarks;

class ProjectBuilder
{
    public static PublishResult Publish(string projectName, PublishScenario scenario) => scenario switch
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

    private static PublishResult Publish(
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
            $"/p:PublishSingleFile={(singleFile ? "true" : "")}",
            $"/p:PublishReadyToRun={(readyToRun ? "true" : "")}",
            "/p:PublishAot=false"
        };

        if (trimLevel != TrimLevel.None)
        {
            args.Add("/p:PublishTrimmed=true");
            args.Add($"/p:TrimMode={GetTrimLevelPropertyValue(trimLevel)}");
        }
        else
        {
            args.Add("/p:PublishTrimmed=false");
        }

        if (!useAppHost)
        {
            args.Add("/p:UseAppHost=false");
        }

        return PublishImpl(projectName, configuration, output, args, runId);
    }

    private static PublishResult PublishAot(
        string projectName,
        string configuration = "Release",
        string? output = null,
        TrimLevel trimLevel = TrimLevel.Default,
        string? runId = null)
    {
        if (trimLevel == TrimLevel.None)
        {
            throw new ArgumentOutOfRangeException(nameof(trimLevel), "'TrimLevel.None' is not supported when publishing for AOT.");
        }

        var args = new List<string>
        {
            "--runtime", RuntimeInformation.RuntimeIdentifier,
            "--self-contained",
            "/p:PublishAot=true",
            "/p:PublishSingleFile=",
            "/p:PublishTrimmed="
        };

        if (trimLevel != TrimLevel.None)
        {
            args.Add($"/p:TrimMode={GetTrimLevelPropertyValue(trimLevel)}");
        }

        return PublishImpl(projectName, configuration, output, args, runId);
    }

    private static PublishResult PublishImpl(string projectName, string configuration = "Release", string? output = null, IEnumerable<string>? args = null, string? runId = null)
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

        //DotNetCli.Clean(cmdArgs);

        cmdArgs.AddRange(new[] { $"--output", output });
        cmdArgs.Add("--disable-build-servers");
        if (args is not null)
        {
            cmdArgs.AddRange(args);
        }

        DotNetCli.Publish(cmdArgs);

        var appFilePath = Path.Join(output, projectName);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            appFilePath += ".exe";
        }

        if (!File.Exists(appFilePath))
        {
            appFilePath = Path.Join(output, projectName + ".dll");
            if (!File.Exists(appFilePath))
            {
                throw new InvalidOperationException($"Could not find application exe or dll '{appFilePath}'");
            }
        }

        return new(appFilePath, GetUserSecretsId(projectPath));
    }

    private static string? GetUserSecretsId(string projectFilePath)
    {
        var xml = XDocument.Load(projectFilePath);
        var userSecretsIdElement = xml.Descendants("UserSecretsId").FirstOrDefault();
        return userSecretsIdElement?.Value;
    }

    private static string GetTrimLevelPropertyValue(TrimLevel trimLevel)
    {
        return trimLevel switch
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

public record PublishResult(string AppFilePath, string? UserSecretsId = null);

enum TrimLevel
{
    None,
    Default,
    Partial,
    Full
}
