using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Masked password input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Secret")]
[ExcludeFromCodeCoverage]
public sealed class SecretConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="SecretConfigurationPropertyType"/>.</summary>
    public SecretConfigurationPropertyType() : base(5, "Secret") { }
}
