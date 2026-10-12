using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Commands;

/// <summary>What object (proc / view / function) contains the given Line in FilePath.</summary>
[TypeOption(typeof(SqlCommands), "GetContainingObject", RestrictToCurrentCompilation = true)]
public sealed class GetContainingObjectCommand : SqlCommandBase
{
    public GetContainingObjectCommand() : base("GetContainingObject", StandardSqlCommandCategories.Navigation,
        "Return the SQL object (procedure / view / function / table) that contains the statement at FilePath + Line.") { }
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
}
