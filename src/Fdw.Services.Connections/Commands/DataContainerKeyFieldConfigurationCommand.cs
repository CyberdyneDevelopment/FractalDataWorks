using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.Commands;

/// <summary>
/// Configuration command for <c>data.DataContainerKeyField</c>.
/// </summary>
/// <remarks>
/// Why it can cascade now: the key field names its parent by DataContainerKeyId rather than by key
/// name, so CascadeCollections can stamp the parent's logical id onto it and the save translator can
/// resolve the physical RowId from the declared Foreign key. A child holding its parent's name could
/// receive neither, which left the caller to write the parent, read its id back, and insert the
/// children by hand — so nothing did.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "DataContainerKeyField")]
public sealed class DataContainerKeyFieldConfigurationCommand
    : ConfigurationCommandBase<DataContainerKeyFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerKeyFieldConfigurationCommand"/> class.</summary>
    public DataContainerKeyFieldConfigurationCommand() : base("DataContainerKeyField") { }
}
