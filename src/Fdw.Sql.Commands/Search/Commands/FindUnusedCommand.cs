using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Search.Commands;

/// <summary>SQL objects defined in the project but never referenced.</summary>
[TypeOption(typeof(SqlCommands), "FindUnused", RestrictToCurrentCompilation = true)]
public sealed class FindUnusedCommand : SqlCommandBase
{
    public FindUnusedCommand() : base("FindUnused", StandardSqlCommandCategories.Search,
        "Find objects defined in the project that no other object references. Candidate dead-code (subject to runtime callers being out-of-scope).") { }
    public int MaxResults { get; set; } = 50;
}
