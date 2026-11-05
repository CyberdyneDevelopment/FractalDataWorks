using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="CalculateTransformationCalculationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "CalculateTransformationCalculationConfiguration")]
public sealed class CalculateTransformationCalculationConfigurationNoValidation : UnvalidatedConfiguration<CalculateTransformationCalculationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CalculateTransformationCalculationConfigurationNoValidation"/> class.</summary>
    public CalculateTransformationCalculationConfigurationNoValidation()
        : base("CalculateTransformationCalculationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
