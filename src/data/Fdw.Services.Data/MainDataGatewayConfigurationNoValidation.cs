using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Data;

/// <summary>States that <see cref="MainDataGatewayConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "MainDataGatewayConfiguration")]
public sealed class MainDataGatewayConfigurationNoValidation : UnvalidatedConfiguration<MainDataGatewayConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MainDataGatewayConfigurationNoValidation"/> class.</summary>
    public MainDataGatewayConfigurationNoValidation()
        : base("MainDataGatewayConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
