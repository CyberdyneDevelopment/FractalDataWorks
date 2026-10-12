using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Commands;

/// <summary>Objects that the named object depends on directly.</summary>
[TypeOption(typeof(SqlCommands), "GetDependencies", RestrictToCurrentCompilation = true)]
public sealed class GetDependenciesCommand : SqlCommandBase
{
    public GetDependenciesCommand() : base("GetDependencies", StandardSqlCommandCategories.Navigation,
        "List objects this object directly depends on. For a procedure: tables it reads/writes + procs it calls + functions it invokes.") { }
    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public bool Transitive { get; set; }
}
