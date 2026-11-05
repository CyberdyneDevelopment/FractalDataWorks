using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="FederatedDataSetConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "FederatedDataSetConfiguration")]
public sealed class FederatedDataSetConfigurationNoValidation : UnvalidatedConfiguration<FederatedDataSetConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="FederatedDataSetConfigurationNoValidation"/> class.</summary>
    public FederatedDataSetConfigurationNoValidation()
        : base("FederatedDataSetConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
