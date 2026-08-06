using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Duration/timespan input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Duration")]
[ExcludeFromCodeCoverage]
public sealed class DurationConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="DurationConfigurationPropertyTypeDto"/>.</summary>
    public DurationConfigurationPropertyTypeDto() : base(9, "Duration") { }
}
