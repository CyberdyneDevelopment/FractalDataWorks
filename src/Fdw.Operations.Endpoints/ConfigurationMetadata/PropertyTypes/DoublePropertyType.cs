using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="double"/> to <see cref="ConfigurationPropertyTypeDtos.Number"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Double")]
public sealed class DoublePropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoublePropertyType"/> class.
    /// </summary>
    public DoublePropertyType() : base(7, "Double", typeof(double), ConfigurationPropertyTypeDtos.Number) { }
}
