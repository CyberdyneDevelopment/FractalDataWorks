using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Project.Commands;

/// <summary>List every schema defined in the loaded .sqlproj.</summary>
[TypeOption(typeof(SqlCommands), "ListSchemas", RestrictToCurrentCompilation = true)]
public sealed class ListSchemasCommand : SqlCommandBase
{
    public ListSchemasCommand()
        : base("ListSchemas", StandardSqlCommandCategories.Project,
               "List every CREATE SCHEMA found across the project's scripts. Use as a quick survey before object-level navigation.")
    {
    }
}
