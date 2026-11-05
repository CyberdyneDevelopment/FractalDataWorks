using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="ThresholdFilterGroupConfiguration"/> is checked by <see cref="ThresholdFilterGroupConfigurationValidator"/> when it is written (FDW-4046).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ThresholdFilterGroupConfiguration")]
public sealed class ThresholdFilterGroupConfigurationValidation : ValidatedConfiguration<ThresholdFilterGroupConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdFilterGroupConfigurationValidation"/> class.</summary>
    public ThresholdFilterGroupConfigurationValidation()
        : base("ThresholdFilterGroupConfiguration", new ThresholdFilterGroupConfigurationValidator())
    {
    }
}
