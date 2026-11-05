using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authentication.Validation;

/// <summary>States that <see cref="SessionAuthenticationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SessionAuthenticationConfiguration")]
public sealed class SessionAuthenticationConfigurationNoValidation : UnvalidatedConfiguration<SessionAuthenticationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SessionAuthenticationConfigurationNoValidation"/> class.</summary>
    public SessionAuthenticationConfigurationNoValidation()
        : base("SessionAuthenticationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
