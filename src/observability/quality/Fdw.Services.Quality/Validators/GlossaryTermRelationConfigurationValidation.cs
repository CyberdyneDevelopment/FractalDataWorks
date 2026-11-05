using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Quality.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>States that <see cref="GlossaryTermRelationConfiguration"/> (and what derives from or implements it) is checked by <see cref="GlossaryTermRelationConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GlossaryTermRelationConfiguration")]
public sealed class GlossaryTermRelationConfigurationValidation : ValidatedConfiguration<GlossaryTermRelationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryTermRelationConfigurationValidation"/> class.</summary>
    public GlossaryTermRelationConfigurationValidation()
        : base("GlossaryTermRelationConfiguration", new GlossaryTermRelationConfigurationValidator())
    {
    }
}
