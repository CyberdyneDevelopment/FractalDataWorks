using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>
/// Configuration command for <c>data.DataContainerKey</c>.
/// </summary>
/// <remarks>
/// Why this exists: the cascade writes a child by looking up the ConfigurationCommands option that
/// claims its type. No option claimed a container key, so saving a container that declared one hit
/// NoChildCommandForType and the key was dropped — which is why data.DataContainerKey held no rows
/// in any database while its parent container and sibling fields were fully populated.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "DataContainerKey")]
public sealed class DataContainerKeyConfigurationCommand : ConfigurationCommandBase<DataContainerKeyConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerKeyConfigurationCommand"/> class.</summary>
    public DataContainerKeyConfigurationCommand() : base("DataContainerKey") { }
}
