using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Build.Commands;

/// <summary>Emit a .dacpac for the loaded project at OutputPath.</summary>
[TypeOption(typeof(SqlCommands), "EmitDacpac", RestrictToCurrentCompilation = true)]
public sealed class EmitDacpacCommand : SqlCommandBase
{
    public EmitDacpacCommand() : base("EmitDacpac", StandardSqlCommandCategories.Build,
        "Emit a .dacpac (compiled schema package) for the loaded project at OutputPath. Useful for SSDT deploy pipelines.") { }
    public string OutputPath { get; set; } = string.Empty;
}
