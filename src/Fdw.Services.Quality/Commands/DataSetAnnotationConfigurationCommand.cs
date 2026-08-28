using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSetAnnotation configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "DataSetAnnotation")]
public sealed class DataSetAnnotationConfigurationCommand : ConfigurationCommandBase<DataSetAnnotationConfiguration>
{
    /// <inheritdoc/>
    public DataSetAnnotationConfigurationCommand() : base("DataSetAnnotation") { }

    /// <inheritdoc/>
    protected override string NameColumn => "DataSetName";
}
