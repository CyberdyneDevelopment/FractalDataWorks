using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataContainerKey configuration domain.</summary>
/// <remarks>
/// Why this exists separately from the POCO: <c>DefaultConfigurationProvider.SaveOneChild</c> resolves a
/// cascade child's write command by CLR-type identity against <c>ConfigurationCommands.All()</c>, and
/// fails loud with <c>NoChildCommandForType</c> when none matches. <c>DataContainerConfiguration.Keys</c>
/// has emitted a cascade descriptor all along, so without this option a container saved with any key
/// reached that failure instead of writing — which is why <c>data.DataContainerKey</c> held no rows.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "DataContainerKey")]
public sealed class DataContainerKeyConfigurationCommand : ConfigurationCommandBase<DataContainerKeyConfiguration>
{
    /// <inheritdoc/>
    public DataContainerKeyConfigurationCommand() : base("DataContainerKey") { }
}
