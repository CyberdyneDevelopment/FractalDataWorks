using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="ThresholdRuleNotificationConfiguration"/> is checked by <see cref="ThresholdRuleNotificationConfigurationValidator"/> when it is written (FDW-4046).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ThresholdRuleNotificationConfiguration")]
public sealed class ThresholdRuleNotificationConfigurationValidation : ValidatedConfiguration<ThresholdRuleNotificationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdRuleNotificationConfigurationValidation"/> class.</summary>
    public ThresholdRuleNotificationConfigurationValidation()
        : base("ThresholdRuleNotificationConfiguration", new ThresholdRuleNotificationConfigurationValidator())
    {
    }
}
