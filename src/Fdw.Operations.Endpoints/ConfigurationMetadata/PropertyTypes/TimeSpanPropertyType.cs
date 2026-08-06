using System;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="System.TimeSpan"/> to <see cref="ConfigurationPropertyTypeDtos.Duration"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "TimeSpan")]
public sealed class TimeSpanPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSpanPropertyType"/> class.
    /// </summary>
    public TimeSpanPropertyType() : base(11, "TimeSpan", typeof(TimeSpan), ConfigurationPropertyTypeDtos.Duration) { }
}
