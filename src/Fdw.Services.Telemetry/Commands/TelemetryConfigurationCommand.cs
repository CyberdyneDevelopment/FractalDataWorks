using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Telemetry.Commands;

/// <summary>Data commands for the logging domain table.</summary>
[TypeOption(typeof(ConfigurationCommands), "Telemetry")]
public sealed class TelemetryConfigurationCommand : ConfigurationCommandBase<TelemetryConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TelemetryConfigurationCommand"/> class.</summary>
    public TelemetryConfigurationCommand() : base("Telemetry") { }
}
