using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Benchmarks;

internal class AppMetricsColumnProvider : IColumnProvider
{
    public IEnumerable<IColumn> GetColumns(Summary summary)
    {
        // TODO: Refactor AppSizeColumn to use AppOutputMetricColumn
        yield return new AppSizeColumn();

        yield return new AppOutputMetricColumn { MetricId = "Environment.WorkingSet", Id = "AppMemory", ColumnName = "App Memory" };
    }

    class AppOutputMetricColumn : IColumn
    {
        public required string MetricId { get; init; }
        public required string Id { get; init; }
        public required string ColumnName { get; init; }
        public bool AlwaysShow { get; } = true;
        public ColumnCategory Category { get; } = ColumnCategory.Custom;
        public int PriorityInCategory { get; } = 0;
        public bool IsNumeric { get; } = true;
        public UnitType UnitType { get; } = UnitType.Size;
        public string Legend => ColumnName;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var projectName = benchmarkCase.Parameters[nameof(StartupTimeBenchmarks.Project)].ToString();
            var runId = benchmarkCase.Parameters[nameof(StartupTimeBenchmarks.PublishKind)].ToString();

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                var publishDir = PathHelper.GetProjectPublishDir(projectName, runId);
                var appOutputFilePath = Path.Combine(publishDir, $"output.txt");

                if (File.Exists(appOutputFilePath))
                {
                    using var outputReader = new StreamReader(new FileStream(appOutputFilePath, FileMode.Open));

                    while (!outputReader.EndOfStream)
                    {
                        var line = outputReader.ReadLine();
                        if (line is not null)
                        {
                            var parts = line.Split(',', StringSplitOptions.TrimEntries);
                            if (parts[0] == MetricId)
                            {
                                if (long.TryParse(parts[2], out var size))
                                {
                                    var mb = size / 1024d / 1024d;
                                    var value = mb.ToString("0.00 MB");
                                    return value;
                                }
                            }
                        }
                    }
                }
            }

            return "";
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    }
}
