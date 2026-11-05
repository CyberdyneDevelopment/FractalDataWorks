using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Quality.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>States that <see cref="QualityRuleImplementationConfiguration"/> is checked by <see cref="QualityRuleConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "QualityRuleImplementationConfiguration")]
public sealed class QualityRuleConfigurationValidation : ValidatedConfiguration<QualityRuleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QualityRuleConfigurationValidation"/> class.</summary>
    public QualityRuleConfigurationValidation()
        : base("QualityRuleImplementationConfiguration", new QualityRuleConfigurationValidator())
    {
    }
}
