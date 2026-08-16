using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataContainerKeyField configuration domain.</summary>
/// <remarks>
/// The ordered participants in a key, cascaded from <c>DataContainerKeyConfiguration.KeyFields</c>. This
/// option only becomes reachable once the key field names its parent by id: the cascade stamps the
/// parent's logical id onto each child by column name, so a child carrying only <c>KeyName</c> receives
/// no stamp and its NOT NULL <c>DataContainerKeyRowId</c> has nothing to resolve at insert. The DDL that
/// adds <c>DataContainerKeyId</c>/<c>DataContainerKeyRowId</c> must be deployed and
/// <c>configurationSchema.json</c> regenerated before a save through this command can link.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "DataContainerKeyField")]
public sealed class DataContainerKeyFieldConfigurationCommand : ConfigurationCommandBase<DataContainerKeyFieldConfiguration>
{
    /// <inheritdoc/>
    public DataContainerKeyFieldConfigurationCommand() : base("DataContainerKeyField") { }
}
