using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>States that <see cref="ExternalIdentityConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ExternalIdentityConfiguration")]
public sealed class ExternalIdentityConfigurationNoValidation : UnvalidatedConfiguration<ExternalIdentityConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityConfigurationNoValidation"/> class.</summary>
    public ExternalIdentityConfigurationNoValidation()
        : base("ExternalIdentityConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
