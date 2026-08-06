using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Commands;

/// <summary>Run T-SQL anti-pattern detectors (SELECT *, cursors, NOLOCK, missing SET NOCOUNT, etc.).</summary>
[TypeOption(typeof(SqlCommands), "DetectAntiPatterns", RestrictToCurrentCompilation = true)]
public sealed class DetectAntiPatternsCommand : SqlCommandBase
{
    public DetectAntiPatternsCommand()
        : base("DetectAntiPatterns", StandardSqlCommandCategories.Analysis,
               "Run a battery of T-SQL anti-pattern detectors (SELECT *, cursors, NOLOCK hints, missing SET NOCOUNT, RBAR loops, etc.) over the workspace or a single file.") { }
    public string? FilePath { get; set; }
}
