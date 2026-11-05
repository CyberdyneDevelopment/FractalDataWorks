using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authentication.Validation;

/// <summary>States that <see cref="JwtBearerAuthenticationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "JwtBearerAuthenticationConfiguration")]
public sealed class JwtBearerAuthenticationConfigurationNoValidation : UnvalidatedConfiguration<JwtBearerAuthenticationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtBearerAuthenticationConfigurationNoValidation"/> class.</summary>
    public JwtBearerAuthenticationConfigurationNoValidation()
        : base("JwtBearerAuthenticationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
