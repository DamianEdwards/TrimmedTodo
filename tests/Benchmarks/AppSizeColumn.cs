using System.Runtime.InteropServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class AppSizeColumn : IColumn
{
    public string Id { get; } = "AppSize";
    public string ColumnName { get; } = "App Size";
    public string Legend { get; } = "Published application size in MB";
    public bool AlwaysShow { get; } = true;
    public ColumnCategory Category { get; } = ColumnCategory.Custom;
    public int PriorityInCategory { get; } = 0;
    public bool IsNumeric { get; } = true;
    public UnitType UnitType { get; } = UnitType.Size;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var projectName = benchmarkCase.Parameters[nameof(StartupTimeBenchmarks.Project)].ToString();
        var runId = benchmarkCase.Parameters[nameof(StartupTimeBenchmarks.PublishKind)].ToString();

        if (!string.IsNullOrWhiteSpace(projectName))
        {
            var publishDir = PathHelper.GetProjectPublishDir(projectName, runId);
            var dllFilePath = Path.Combine(publishDir, $"{projectName}.dll");
            var exeFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(publishDir, $"{projectName}.exe")
                : Path.Combine(publishDir, projectName);

            long size = 0;
            if (File.Exists(dllFilePath))
            {
                var dirInfo = new DirectoryInfo(publishDir);
                size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
            else if (File.Exists(exeFilePath))
            {
                size = (new FileInfo(exeFilePath)).Length;
            }

            var mb = size / 1024d / 1024d;
            var value = mb.ToString("0.00 MB");
            return value;
        }

        return "";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

    public bool IsAvailable(Summary summary) => true;
    //    summary.BenchmarksCases.Any(benchmarkCase =>
    //{
    //    var projectName = benchmarkCase.Parameters[nameof(StartupTimeBenchmarks.ProjectName)].ToString();
    //    var runId = benchmarkCase.Parameters[nameof(StartupTimeBenchmarks.Scenario)].ToString();
    //    return projectName is not null && runId is not null && Directory.Exists(PathHelper.GetProjectPublishDir(projectName, runId));
    //});

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}
