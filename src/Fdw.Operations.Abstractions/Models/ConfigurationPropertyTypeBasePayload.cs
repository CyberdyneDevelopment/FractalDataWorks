using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>Base class for configuration property types for UI rendering.</summary>
public abstract class ConfigurationPropertyTypeBasePayload : TypeOptionBase<ConfigurationPropertyTypeBasePayload>, IConfigurationPropertyType
{
    /// <summary>Initializes a new instance of <see cref="ConfigurationPropertyTypeBasePayload"/>.</summary>
    protected ConfigurationPropertyTypeBasePayload(string name) : base(name) { }
}
