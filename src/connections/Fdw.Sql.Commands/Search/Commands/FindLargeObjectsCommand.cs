using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Search.Commands;

/// <summary>Surface objects (procs/views/functions) by line count, highest first.</summary>
[TypeOption(typeof(SqlCommands), "FindLargeObjects", RestrictToCurrentCompilation = true)]
public sealed class FindLargeObjectsCommand : SqlCommandBase
{
    public FindLargeObjectsCommand() : base("FindLargeObjects", StandardSqlCommandCategories.Search,
        "Rank objects by line count. Use to triage refactor candidates — large procs are often hiding multiple responsibilities.") { }
    public int MinLines { get; set; } = 100;
    public int MaxResults { get; set; } = 25;
}
