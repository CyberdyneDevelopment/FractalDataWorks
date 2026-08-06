using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="long"/> to <see cref="ConfigurationPropertyTypeDtos.WholeNumber"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Long")]
public sealed class LongPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LongPropertyType"/> class.
    /// </summary>
    public LongPropertyType() : base(3, "Long", typeof(long), ConfigurationPropertyTypeDtos.WholeNumber) { }
}
