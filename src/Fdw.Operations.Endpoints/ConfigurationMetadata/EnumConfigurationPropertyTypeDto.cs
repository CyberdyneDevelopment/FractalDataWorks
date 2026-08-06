using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Dropdown selection from AllowedValues.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Enum")]
[ExcludeFromCodeCoverage]
public sealed class EnumConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="EnumConfigurationPropertyTypeDto"/>.</summary>
    public EnumConfigurationPropertyTypeDto() : base(6, "Enum") { }
}
