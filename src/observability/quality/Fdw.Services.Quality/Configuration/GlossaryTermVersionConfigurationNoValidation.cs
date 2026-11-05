using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Configuration;

/// <summary>States that <see cref="GlossaryTermVersionImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GlossaryTermVersionImplementationConfiguration")]
public sealed class GlossaryTermVersionConfigurationNoValidation : UnvalidatedConfiguration<GlossaryTermVersionImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryTermVersionConfigurationNoValidation"/> class.</summary>
    public GlossaryTermVersionConfigurationNoValidation()
        : base("GlossaryTermVersionImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
