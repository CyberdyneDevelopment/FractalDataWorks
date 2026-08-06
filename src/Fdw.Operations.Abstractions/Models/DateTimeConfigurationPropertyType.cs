using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Date/time picker.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="DateTimeConfigurationPropertyType"/>.</summary>
    public DateTimeConfigurationPropertyType() : base(8, "DateTime") { }
}
