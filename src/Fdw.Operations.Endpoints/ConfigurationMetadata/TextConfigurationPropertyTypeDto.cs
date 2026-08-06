using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Standard text input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="TextConfigurationPropertyTypeDto"/>.</summary>
    public TextConfigurationPropertyTypeDto() : base(1, "Text") { }
}
