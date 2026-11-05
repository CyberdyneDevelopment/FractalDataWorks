using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="ThresholdParameterConfiguration"/> is checked by <see cref="ThresholdParameterConfigurationValidator"/> when it is written (FDW-4046).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ThresholdParameterConfiguration")]
public sealed class ThresholdParameterConfigurationValidation : ValidatedConfiguration<ThresholdParameterConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdParameterConfigurationValidation"/> class.</summary>
    public ThresholdParameterConfigurationValidation()
        : base("ThresholdParameterConfiguration", new ThresholdParameterConfigurationValidator())
    {
    }
}
