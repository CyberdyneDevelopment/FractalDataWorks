using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authentication.Flow;

/// <summary>States that <see cref="AuthenticationFlowImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "AuthenticationFlowImplementationConfiguration")]
public sealed class AuthenticationFlowConfigurationNoValidation : UnvalidatedConfiguration<AuthenticationFlowImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowConfigurationNoValidation"/> class.</summary>
    public AuthenticationFlowConfigurationNoValidation()
        : base("AuthenticationFlowImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
