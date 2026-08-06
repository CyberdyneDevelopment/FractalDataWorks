using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Boolean toggle/checkbox.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Bool")]
[ExcludeFromCodeCoverage]
public sealed class BoolConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="BoolConfigurationPropertyTypeDto"/>.</summary>
    public BoolConfigurationPropertyTypeDto() : base(4, "Bool") { }
}
