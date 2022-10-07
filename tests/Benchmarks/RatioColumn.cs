using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Benchmarks;

public class ParameterRatioColumn : IColumn
{
    private readonly string _parameterName;
    private readonly BaselineKind _baselineKind;
    private readonly object? _baselineValue;

    public ParameterRatioColumn(string parameterName, object? baselineValue)
        : this(parameterName, BaselineKind.Equals, baselineValue)
    {

    }

    public ParameterRatioColumn(string parameterName, BaselineKind baselineKind, object? baselineValue = null)
    {
        if (baselineKind == BaselineKind.StartsWith && baselineValue is not null)
        {
            throw new ArgumentException($"Specifiying a baseline value for {nameof(BaselineKind)}.{nameof(BaselineKind.StartsWith)} is not supported.", nameof(baselineValue));
        }

        _parameterName = parameterName;
        _baselineKind = baselineKind;
        _baselineValue = baselineValue;

        Id = $"Ratio_Parameter_{parameterName}";
        ColumnName = $"Ratio ({parameterName})";
        Legend = $"[Current{_parameterName}]/[{baselineValue}]";
    }

    public string Id { get; }
    public string ColumnName { get; }
    public string Legend { get; }
    public bool AlwaysShow { get; } = false;
    public ColumnCategory Category { get; } = ColumnCategory.Custom;
    public int PriorityInCategory { get; } = 0;
    public bool IsNumeric { get; } = true;
    public UnitType UnitType { get; } = UnitType.Dimensionless;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        BenchmarkCase? baseline = null;

        if (_baselineKind == BaselineKind.StartsWith)
        {
            // Value is like "TrimmedTodo.MinimalApi.Sqlite"
            // Find case with shortest matching value
            var baselineCandidates = new HashSet<BenchmarkCase>();
            var valueParts = ((string)benchmarkCase.Parameters[_parameterName]).Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var currentPrefix = "";
            foreach (var part in valueParts)
            {
                currentPrefix += part + '.';

                var baselineCaseMatches = summary.BenchmarksCases.Where(c => ((string)c.Parameters[_parameterName]).StartsWith(currentPrefix)).ToList();
                foreach (var match in baselineCaseMatches)
                {
                    baselineCandidates.Add(match);
                }
            }

            baseline = baselineCandidates.MinBy(c => ((string)c.Parameters[_parameterName]).Length);
        }
        else if (_baselineKind == BaselineKind.Equals)
        {
            baseline = summary.BenchmarksCases
                .SingleOrDefault(c =>
                {
                    var value = c.Parameters[_parameterName];

                    if (!value.Equals(_baselineValue))
                    {
                        return false;
                    }

                    for (var i = 0; i < benchmarkCase.Parameters.Count; i++)
                    {
                        var p = benchmarkCase.Parameters[i];

                        // Skip parameter
                        if (p.Name.Equals(_parameterName, StringComparison.Ordinal)) continue;

                        if (!c.Parameters[p.Name].Equals(p.Value))
                        {
                            // Parameter doesn't match
                            return false;
                        }
                    }

                    return true;
                });
        }

        if (baseline is not null)
        {
            var baselineReport = summary.Reports.First(r => r.BenchmarkCase == baseline);
            var report = summary.Reports.First(r => r.BenchmarkCase == benchmarkCase);

            return (report.ResultStatistics.Mean / baselineReport.ResultStatistics.Mean).ToString("#.00");
        }

        return "?";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

    public bool IsAvailable(Summary summary)
    {
        var hasBaselineCase = summary.BenchmarksCases.Any(c =>
            {
                var parameterValue = c.Parameters[_parameterName];
                return _baselineKind switch
                {
                    BaselineKind.Equals => parameterValue.Equals(_baselineValue),
                    BaselineKind.StartsWith => parameterValue is string pv &&
                        summary.BenchmarksCases.Any(c2 =>
                            c2.Parameters[_parameterName] is string value
                            && value.StartsWith(pv.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0])),
                    _ => throw new InvalidOperationException($"Unsupported {nameof(BaselineKind)} specified")
                };
            });
        return hasBaselineCase;
    }

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    private string GetStartsWithPrefix(BenchmarkCase benchmarkCase) =>
        ((string)benchmarkCase.Parameters[_parameterName]).Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];

    public enum BaselineKind
    {
        Equals,
        StartsWith
    }
}
