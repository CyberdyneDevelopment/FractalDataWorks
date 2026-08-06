using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Masked password input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Secret")]
[ExcludeFromCodeCoverage]
public sealed class SecretConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="SecretConfigurationPropertyTypeDto"/>.</summary>
    public SecretConfigurationPropertyTypeDto() : base(5, "Secret") { }
}
