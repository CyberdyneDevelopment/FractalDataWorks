using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Quality.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>States that <see cref="GlossaryTermImplementationConfiguration"/> is checked by <see cref="GlossaryTermConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GlossaryTermImplementationConfiguration")]
public sealed class GlossaryTermConfigurationValidation : ValidatedConfiguration<GlossaryTermImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryTermConfigurationValidation"/> class.</summary>
    public GlossaryTermConfigurationValidation()
        : base("GlossaryTermImplementationConfiguration", new GlossaryTermConfigurationValidator())
    {
    }
}
