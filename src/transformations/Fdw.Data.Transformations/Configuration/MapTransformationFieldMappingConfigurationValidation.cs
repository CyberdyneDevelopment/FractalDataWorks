using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// States that <see cref="MapTransformationFieldMappingConfiguration"/> is checked by <see cref="MapTransformationFieldMappingConfigurationValidator"/>
/// when it is written (FDW-3541, FDW-3823).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "MapTransformationFieldMappingConfiguration")]
public sealed class MapTransformationFieldMappingConfigurationValidation : ValidatedConfiguration<MapTransformationFieldMappingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MapTransformationFieldMappingConfigurationValidation"/> class.</summary>
    public MapTransformationFieldMappingConfigurationValidation()
        : base("MapTransformationFieldMappingConfiguration", new MapTransformationFieldMappingConfigurationValidator())
    {
    }
}
