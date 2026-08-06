using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="int"/> to <see cref="ConfigurationPropertyTypeDtos.WholeNumber"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Int")]
public sealed class IntPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntPropertyType"/> class.
    /// </summary>
    public IntPropertyType() : base(2, "Int", typeof(int), ConfigurationPropertyTypeDtos.WholeNumber) { }
}
