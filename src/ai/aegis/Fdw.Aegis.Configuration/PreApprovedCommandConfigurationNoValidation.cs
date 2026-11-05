using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Aegis.Configuration;

/// <summary>States that <see cref="PreApprovedCommandConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PreApprovedCommandConfiguration")]
public sealed class PreApprovedCommandConfigurationNoValidation : UnvalidatedConfiguration<PreApprovedCommandConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PreApprovedCommandConfigurationNoValidation"/> class.</summary>
    public PreApprovedCommandConfigurationNoValidation()
        : base("PreApprovedCommandConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
