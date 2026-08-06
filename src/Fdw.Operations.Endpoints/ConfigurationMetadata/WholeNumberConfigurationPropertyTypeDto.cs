using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Integer numeric input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "WholeNumber")]
[ExcludeFromCodeCoverage]
public sealed class WholeNumberConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="WholeNumberConfigurationPropertyTypeDto"/>.</summary>
    public WholeNumberConfigurationPropertyTypeDto() : base(2, "WholeNumber") { }
}
