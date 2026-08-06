using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="byte"/> to <see cref="ConfigurationPropertyTypeDtos.WholeNumber"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Byte")]
public sealed class BytePropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BytePropertyType"/> class.
    /// </summary>
    public BytePropertyType() : base(5, "Byte", typeof(byte), ConfigurationPropertyTypeDtos.WholeNumber) { }
}
