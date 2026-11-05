using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Quality.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>States that <see cref="GlossaryTermLinkedDataSetConfiguration"/> (and what derives from or implements it) is checked by <see cref="GlossaryTermLinkedDataSetConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GlossaryTermLinkedDataSetConfiguration")]
public sealed class GlossaryTermLinkedDataSetConfigurationValidation : ValidatedConfiguration<GlossaryTermLinkedDataSetConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryTermLinkedDataSetConfigurationValidation"/> class.</summary>
    public GlossaryTermLinkedDataSetConfigurationValidation()
        : base("GlossaryTermLinkedDataSetConfiguration", new GlossaryTermLinkedDataSetConfigurationValidator())
    {
    }
}
