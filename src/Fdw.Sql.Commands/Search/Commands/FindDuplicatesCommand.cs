using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Search.Commands;

/// <summary>Detect token-equivalent SQL blocks across the workspace.</summary>
[TypeOption(typeof(SqlCommands), "FindDuplicates", RestrictToCurrentCompilation = true)]
public sealed class FindDuplicatesCommand : SqlCommandBase
{
    public FindDuplicatesCommand() : base("FindDuplicates", StandardSqlCommandCategories.Search,
        "Detect copy-pasted SQL blocks across the workspace using a token-hash. Tune MinLines + MinTokens for sensitivity.") { }
    public int MinLines { get; set; } = 6;
    public int MinTokens { get; set; } = 25;
}
