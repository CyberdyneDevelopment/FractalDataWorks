using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Configuration;

/// <summary>
/// TypeCollection of configuration command options. Each TypeOption is a concrete subclass
/// of ConfigurationCommandBase&lt;TConfig&gt; (one per configuration domain: Connection, DataStore,
/// DataSet, etc). Source generator produces ByType(Type) and All() static lookups.
/// </summary>
[TypeCollection(typeof(ConfigurationCommandBase<>), typeof(IConfigurationCommands), typeof(ConfigurationCommands))]
public abstract partial class ConfigurationCommands : TypeCollectionBase<IConfigurationCommands, IConfigurationCommands>
{
}
