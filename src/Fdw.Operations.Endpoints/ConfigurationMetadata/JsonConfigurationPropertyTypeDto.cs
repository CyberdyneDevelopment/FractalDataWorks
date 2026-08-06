using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Raw JSON editor.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Json")]
[ExcludeFromCodeCoverage]
public sealed class JsonConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="JsonConfigurationPropertyTypeDto"/>.</summary>
    public JsonConfigurationPropertyTypeDto() : base(10, "Json") { }
}
