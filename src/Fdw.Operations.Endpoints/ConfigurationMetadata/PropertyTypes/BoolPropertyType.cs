using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="bool"/> to <see cref="ConfigurationPropertyTypeDtos.Bool"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Bool")]
public sealed class BoolPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoolPropertyType"/> class.
    /// </summary>
    public BoolPropertyType() : base(1, "Bool", typeof(bool), ConfigurationPropertyTypeDtos.Bool) { }
}
