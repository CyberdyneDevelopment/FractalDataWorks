using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Decimal numeric input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="NumberConfigurationPropertyTypeDto"/>.</summary>
    public NumberConfigurationPropertyTypeDto() : base(3, "Number") { }
}
