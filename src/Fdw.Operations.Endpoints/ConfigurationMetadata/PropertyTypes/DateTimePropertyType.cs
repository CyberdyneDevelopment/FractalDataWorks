using System;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Maps <see cref="System.DateTime"/> to <see cref="ConfigurationPropertyTypeDtos.DateTime"/>.
/// </summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "DateTime")]
public sealed class DateTimePropertyType : ConfigurationPropertyTypeBaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimePropertyType"/> class.
    /// </summary>
    public DateTimePropertyType() : base(9, "DateTime", typeof(DateTime), ConfigurationPropertyTypeDtos.DateTime) { }
}
