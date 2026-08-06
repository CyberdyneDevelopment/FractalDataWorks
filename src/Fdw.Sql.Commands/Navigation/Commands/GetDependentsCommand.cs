using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Commands;

/// <summary>Objects that depend on the named object (reverse-dependency lookup).</summary>
[TypeOption(typeof(SqlCommands), "GetDependents", RestrictToCurrentCompilation = true)]
public sealed class GetDependentsCommand : SqlCommandBase
{
    public GetDependentsCommand() : base("GetDependents", StandardSqlCommandCategories.Navigation,
        "List objects that depend on this object. Useful before dropping a table or changing a procedure signature.") { }
    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public bool Transitive { get; set; }
}
