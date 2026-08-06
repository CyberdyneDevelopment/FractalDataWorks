using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="short"/> to <see cref="ConfigurationPropertyTypeDtos.WholeNumber"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Short")]
public sealed class ShortPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShortPropertyType"/> class.
    /// </summary>
    public ShortPropertyType() : base(4, "Short", typeof(short), ConfigurationPropertyTypeDtos.WholeNumber) { }
}
