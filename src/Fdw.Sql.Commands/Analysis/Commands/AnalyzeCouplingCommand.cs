using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Commands;

/// <summary>Afferent / efferent / instability metrics for a SQL object.</summary>
[TypeOption(typeof(SqlCommands), "AnalyzeCoupling", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeCouplingCommand : SqlCommandBase
{
    public AnalyzeCouplingCommand()
        : base("AnalyzeCoupling", StandardSqlCommandCategories.Analysis,
               "Compute coupling metrics for the object: efferent (what it depends on), afferent (what depends on it), instability.") { }
    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
