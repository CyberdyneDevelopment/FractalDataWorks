using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="float"/> to <see cref="ConfigurationPropertyTypeDtos.Number"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Float")]
public sealed class FloatPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatPropertyType"/> class.
    /// </summary>
    public FloatPropertyType() : base(8, "Float", typeof(float), ConfigurationPropertyTypeDtos.Number) { }
}
