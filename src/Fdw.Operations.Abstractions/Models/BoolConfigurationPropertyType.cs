using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Boolean toggle/checkbox.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Bool")]
[ExcludeFromCodeCoverage]
public sealed class BoolConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="BoolConfigurationPropertyType"/>.</summary>
    public BoolConfigurationPropertyType() : base(4, "Bool") { }
}
