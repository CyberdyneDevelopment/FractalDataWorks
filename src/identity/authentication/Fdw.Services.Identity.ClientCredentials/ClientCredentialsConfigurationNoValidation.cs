using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>States that <see cref="ClientCredentialsConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ClientCredentialsConfiguration")]
public sealed class ClientCredentialsConfigurationNoValidation : UnvalidatedConfiguration<ClientCredentialsConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ClientCredentialsConfigurationNoValidation"/> class.</summary>
    public ClientCredentialsConfigurationNoValidation()
        : base("ClientCredentialsConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
