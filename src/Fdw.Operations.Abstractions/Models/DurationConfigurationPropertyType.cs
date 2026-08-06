using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Duration/timespan input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Duration")]
[ExcludeFromCodeCoverage]
public sealed class DurationConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="DurationConfigurationPropertyType"/>.</summary>
    public DurationConfigurationPropertyType() : base(9, "Duration") { }
}
