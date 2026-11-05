using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="ThresholdRuleConfiguration"/> is checked by <see cref="ThresholdRuleConfigurationValidator"/> when it is written (FDW-4046).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ThresholdRuleConfiguration")]
public sealed class ThresholdRuleConfigurationValidation : ValidatedConfiguration<ThresholdRuleConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdRuleConfigurationValidation"/> class.</summary>
    public ThresholdRuleConfigurationValidation()
        : base("ThresholdRuleConfiguration", new ThresholdRuleConfigurationValidator())
    {
    }
}
