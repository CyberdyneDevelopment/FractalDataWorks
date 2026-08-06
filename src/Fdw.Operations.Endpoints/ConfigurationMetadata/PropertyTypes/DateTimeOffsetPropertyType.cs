using System;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="System.DateTimeOffset"/> to <see cref="ConfigurationPropertyTypeDtos.DateTime"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "DateTimeOffset")]
public sealed class DateTimeOffsetPropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeOffsetPropertyType"/> class.
    /// </summary>
    public DateTimeOffsetPropertyType() : base(10, "DateTimeOffset", typeof(DateTimeOffset), ConfigurationPropertyTypeDtos.DateTime) { }
}
