using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Commands;

/// <summary>Locate the CREATE statement for an object.</summary>
[TypeOption(typeof(SqlCommands), "FindDefinition", RestrictToCurrentCompilation = true)]
public sealed class FindDefinitionCommand : SqlCommandBase
{
    public FindDefinitionCommand() : base("FindDefinition", StandardSqlCommandCategories.Navigation,
        "Locate the CREATE statement that defines the named object. Returns file/line of the definition.") { }
    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
