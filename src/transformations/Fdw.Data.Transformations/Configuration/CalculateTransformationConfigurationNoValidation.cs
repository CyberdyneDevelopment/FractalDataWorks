using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="CalculateTransformationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "CalculateTransformationConfiguration")]
public sealed class CalculateTransformationConfigurationNoValidation : UnvalidatedConfiguration<CalculateTransformationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CalculateTransformationConfigurationNoValidation"/> class.</summary>
    public CalculateTransformationConfigurationNoValidation()
        : base("CalculateTransformationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
