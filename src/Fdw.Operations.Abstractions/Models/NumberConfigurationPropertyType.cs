using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Decimal numeric input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="NumberConfigurationPropertyType"/>.</summary>
    public NumberConfigurationPropertyType() : base(3, "Number") { }
}
