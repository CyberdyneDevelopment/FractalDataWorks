using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authentication.Flow;

/// <summary>States that <see cref="AuthenticationFlowStepConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "AuthenticationFlowStepConfiguration")]
public sealed class AuthenticationFlowStepConfigurationNoValidation : UnvalidatedConfiguration<AuthenticationFlowStepConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowStepConfigurationNoValidation"/> class.</summary>
    public AuthenticationFlowStepConfigurationNoValidation()
        : base("AuthenticationFlowStepConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
