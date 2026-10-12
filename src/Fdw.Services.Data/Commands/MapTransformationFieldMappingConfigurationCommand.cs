using Fdw.Collections.Attributes;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>Reads and writes <c>transform.MapTransformationConfigurationFieldMapping</c> rows as <see cref="MapTransformationFieldMappingConfiguration"/>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "MapTransformationConfigurationFieldMapping")]
public sealed class MapTransformationFieldMappingConfigurationCommand : ConfigurationCommandBase<MapTransformationFieldMappingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MapTransformationFieldMappingConfigurationCommand"/> class.</summary>
    public MapTransformationFieldMappingConfigurationCommand() : base("MapTransformationConfigurationFieldMapping") { }
}
