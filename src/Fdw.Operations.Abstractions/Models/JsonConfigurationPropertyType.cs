using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Raw JSON editor.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Json")]
[ExcludeFromCodeCoverage]
public sealed class JsonConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="JsonConfigurationPropertyType"/>.</summary>
    public JsonConfigurationPropertyType() : base(10, "Json") { }
}
