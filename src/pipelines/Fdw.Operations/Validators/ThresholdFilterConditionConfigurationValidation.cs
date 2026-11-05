using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="ThresholdFilterConditionConfiguration"/> is checked by <see cref="ThresholdFilterConditionConfigurationValidator"/> when it is written (FDW-4046).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ThresholdFilterConditionConfiguration")]
public sealed class ThresholdFilterConditionConfigurationValidation : ValidatedConfiguration<ThresholdFilterConditionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdFilterConditionConfigurationValidation"/> class.</summary>
    public ThresholdFilterConditionConfigurationValidation()
        : base("ThresholdFilterConditionConfiguration", new ThresholdFilterConditionConfigurationValidator())
    {
    }
}
