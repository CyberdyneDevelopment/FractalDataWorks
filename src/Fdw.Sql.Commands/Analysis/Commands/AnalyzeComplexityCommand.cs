using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Commands;

/// <summary>Cyclomatic-style complexity per procedure / function. Flags items over Threshold.</summary>
[TypeOption(typeof(SqlCommands), "AnalyzeComplexity", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeComplexityCommand : SqlCommandBase
{
    public AnalyzeComplexityCommand()
        : base("AnalyzeComplexity", StandardSqlCommandCategories.Analysis,
               "Compute complexity per procedure / function (branch + loop count). Items over Threshold are flagged as ExceedsThreshold.") { }
    public int Threshold { get; set; } = 10;
    public string? FilePath { get; set; }
}
