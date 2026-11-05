using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Hosts.Configuration;

/// <summary>States that <see cref="HostImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "HostImplementationConfiguration")]
public sealed class HostConfigurationNoValidation : UnvalidatedConfiguration<HostImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HostConfigurationNoValidation"/> class.</summary>
    public HostConfigurationNoValidation()
        : base("HostImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
