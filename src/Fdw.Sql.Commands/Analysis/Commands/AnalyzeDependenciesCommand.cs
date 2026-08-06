using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Commands;

/// <summary>Direct objects that ObjectName depends on (tables it reads/writes, procs it calls).</summary>
[TypeOption(typeof(SqlCommands), "AnalyzeDependencies", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeDependenciesCommand : SqlCommandBase
{
    public AnalyzeDependenciesCommand()
        : base("AnalyzeDependencies", StandardSqlCommandCategories.Analysis,
               "List the SQL objects that the given object directly depends on (tables it reads/writes, procs it calls, functions it invokes).") { }
    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
