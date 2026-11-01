using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Pipelines.Scripts.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the parameters of a stored SQL script. Targets the pipe.SqlScriptParameter table.
/// </summary>
/// <remarks>
/// Why: the configuration read and cascade-save resolve the command for a type via <c>ConfigurationCommands.All()</c>; without this
/// TypeOption they fail loud when a stored script is read or saved.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "SqlScriptParameter")]
public sealed class SqlScriptParameterConfigurationCommand : ConfigurationCommandBase<SqlScriptParameterConfiguration>
{
    /// <summary>Initializes the command targeting the SqlScriptParameter table.</summary>
    public SqlScriptParameterConfigurationCommand() : base("SqlScriptParameter") { }
}
