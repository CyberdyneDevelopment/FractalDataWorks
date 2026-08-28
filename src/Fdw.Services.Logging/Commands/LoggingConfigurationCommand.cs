using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Logging.Commands;

/// <summary>Data commands for the logging domain table.</summary>
[TypeOption(typeof(ConfigurationCommands), "Logging")]
public sealed class LoggingConfigurationCommand : ConfigurationCommandBase<LoggingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LoggingConfigurationCommand"/> class.</summary>
    public LoggingConfigurationCommand() : base("Logging") { }
}
