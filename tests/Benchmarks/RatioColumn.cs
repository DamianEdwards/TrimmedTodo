using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Benchmarks;

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
