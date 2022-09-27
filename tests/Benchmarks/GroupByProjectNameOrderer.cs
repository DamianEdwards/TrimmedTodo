using System.Collections.Immutable;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Benchmarks;
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
