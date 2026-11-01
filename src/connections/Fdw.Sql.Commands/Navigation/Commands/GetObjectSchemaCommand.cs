using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Navigation.Commands;

/// <summary>Resolve which schema an object lives in.</summary>
[TypeOption(typeof(SqlCommands), "GetObjectSchema", RestrictToCurrentCompilation = true)]
public sealed class GetObjectSchemaCommand : SqlCommandBase
{
    public GetObjectSchemaCommand() : base("GetObjectSchema", StandardSqlCommandCategories.Navigation,
        "Resolve which schema an object lives in. Use as a quick scope check before referencing an object across scripts.") { }
    public string ObjectName { get; set; } = string.Empty;
}
