using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.LanguageModels.Abstractions;
using Fdw.Validation.Configuration;

namespace Fdw.Services.LanguageModels.Validators;

/// <summary>
/// States that <see cref="ILanguageModelImplementationConfiguration"/> (and what derives from or implements it, so every provider's row) is checked by
/// <see cref="LanguageModelConfigurationValidator"/> when it is written.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ILanguageModelImplementationConfiguration")]
public sealed class LanguageModelConfigurationValidation : ValidatedConfiguration<ILanguageModelImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelConfigurationValidation"/> class.</summary>
    public LanguageModelConfigurationValidation()
        : base("ILanguageModelImplementationConfiguration", new LanguageModelConfigurationValidator())
    {
    }
}
