using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.TokenManagers;

/// <summary>States that <see cref="JwtTokenManagerConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "JwtTokenManagerConfiguration")]
public sealed class JwtTokenManagerConfigurationNoValidation : UnvalidatedConfiguration<JwtTokenManagerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtTokenManagerConfigurationNoValidation"/> class.</summary>
    public JwtTokenManagerConfigurationNoValidation()
        : base("JwtTokenManagerConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
