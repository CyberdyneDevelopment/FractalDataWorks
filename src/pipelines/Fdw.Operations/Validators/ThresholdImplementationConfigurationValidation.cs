using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="ThresholdImplementationConfiguration"/> is checked by <see cref="ThresholdImplementationConfigurationValidator"/> when it is written (FDW-4046).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ThresholdImplementationConfiguration")]
public sealed class ThresholdImplementationConfigurationValidation : ValidatedConfiguration<ThresholdImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdImplementationConfigurationValidation"/> class.</summary>
    public ThresholdImplementationConfigurationValidation()
        : base("ThresholdImplementationConfiguration", new ThresholdImplementationConfigurationValidator())
    {
    }
}
