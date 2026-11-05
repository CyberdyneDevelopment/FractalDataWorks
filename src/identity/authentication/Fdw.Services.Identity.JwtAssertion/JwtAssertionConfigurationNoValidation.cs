using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>States that <see cref="JwtAssertionConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "JwtAssertionConfiguration")]
public sealed class JwtAssertionConfigurationNoValidation : UnvalidatedConfiguration<JwtAssertionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtAssertionConfigurationNoValidation"/> class.</summary>
    public JwtAssertionConfigurationNoValidation()
        : base("JwtAssertionConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
