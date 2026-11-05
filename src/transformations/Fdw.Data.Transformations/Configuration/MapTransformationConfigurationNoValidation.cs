using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="MapTransformationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "MapTransformationConfiguration")]
public sealed class MapTransformationConfigurationNoValidation : UnvalidatedConfiguration<MapTransformationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MapTransformationConfigurationNoValidation"/> class.</summary>
    public MapTransformationConfigurationNoValidation()
        : base("MapTransformationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
