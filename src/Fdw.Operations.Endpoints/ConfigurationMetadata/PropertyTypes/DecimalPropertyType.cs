using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="decimal"/> to <see cref="ConfigurationPropertyTypeDtos.Number"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Decimal")]
public sealed class DecimalPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalPropertyType"/> class.
    /// </summary>
    public DecimalPropertyType() : base(6, "Decimal", typeof(decimal), ConfigurationPropertyTypeDtos.Number) { }
}
