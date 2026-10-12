using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Commands;

/// <summary>Every place an object is referenced across the workspace.</summary>
[TypeOption(typeof(SqlCommands), "FindReferences", RestrictToCurrentCompilation = true)]
public sealed class FindReferencesCommand : SqlCommandBase
{
    public FindReferencesCommand() : base("FindReferences", StandardSqlCommandCategories.Navigation,
        "Find every place an object (table / view / procedure / function / column) is referenced across the workspace. Use to assess blast radius before renaming or dropping.") { }
    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
