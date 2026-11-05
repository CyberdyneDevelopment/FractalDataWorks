using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Themes.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.UI.Themes.Validators;

/// <summary>States that <see cref="ThemeImplementationConfiguration"/> is checked by <see cref="ThemeConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ThemeImplementationConfiguration")]
public sealed class ThemeConfigurationValidation : ValidatedConfiguration<ThemeImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThemeConfigurationValidation"/> class.</summary>
    public ThemeConfigurationValidation()
        : base("ThemeImplementationConfiguration", new ThemeConfigurationValidator())
    {
    }
}
