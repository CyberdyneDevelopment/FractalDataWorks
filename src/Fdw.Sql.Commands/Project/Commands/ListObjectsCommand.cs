using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Project.Commands;

/// <summary>List every object in the loaded .sqlproj — tables, views, procedures, functions, types — optionally filtered by kind.</summary>
[TypeOption(typeof(SqlCommands), "ListObjects", RestrictToCurrentCompilation = true)]
public sealed class ListObjectsCommand : SqlCommandBase
{
    public ListObjectsCommand()
        : base("ListObjects", StandardSqlCommandCategories.Project,
               "List every object (tables, views, procs, funcs, types) in the loaded .sqlproj. Use as the first orientation step after load_sqlproject. Filter by ObjectKind to narrow; null = all kinds.")
    {
    }

    /// <summary>Optional kind filter: Table / View / Procedure / Function / Type. Null or empty = all kinds.</summary>
    public string? ObjectKind { get; set; }

    /// <summary>Optional schema filter (e.g. "dbo"). Null or empty = all schemas.</summary>
    public string? Schema { get; set; }
}
