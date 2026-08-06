using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Date/time picker.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="DateTimeConfigurationPropertyTypeDto"/>.</summary>
    public DateTimeConfigurationPropertyTypeDto() : base(8, "DateTime") { }
}
