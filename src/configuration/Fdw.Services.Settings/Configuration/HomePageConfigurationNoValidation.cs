using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Settings.Configuration;

/// <summary>States that <see cref="HomePageImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "HomePageImplementationConfiguration")]
public sealed class HomePageConfigurationNoValidation : UnvalidatedConfiguration<HomePageImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HomePageConfigurationNoValidation"/> class.</summary>
    public HomePageConfigurationNoValidation()
        : base("HomePageImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
