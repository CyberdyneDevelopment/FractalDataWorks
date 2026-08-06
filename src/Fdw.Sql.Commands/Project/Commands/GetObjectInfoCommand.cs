using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Project.Commands;

/// <summary>Return rich metadata for a single SQL object: kind, schema, definition script, dependencies.</summary>
[TypeOption(typeof(SqlCommands), "GetObjectInfo", RestrictToCurrentCompilation = true)]
public sealed class GetObjectInfoCommand : SqlCommandBase
{
    public GetObjectInfoCommand()
        : base("GetObjectInfo", StandardSqlCommandCategories.Project,
               "Return metadata for one SQL object: kind, schema, defining script path, and direct dependencies. Use after ListObjects to inspect a specific table / view / procedure.")
    {
    }

    public string ObjectName { get; set; } = string.Empty;
    public string? Schema { get; set; }
}
