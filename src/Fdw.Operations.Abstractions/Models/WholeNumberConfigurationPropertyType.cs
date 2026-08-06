using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Integer numeric input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "WholeNumber")]
[ExcludeFromCodeCoverage]
public sealed class WholeNumberConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="WholeNumberConfigurationPropertyType"/>.</summary>
    public WholeNumberConfigurationPropertyType() : base(2, "WholeNumber") { }
}
