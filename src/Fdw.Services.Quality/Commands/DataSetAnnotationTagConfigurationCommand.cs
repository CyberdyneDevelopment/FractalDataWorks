using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the DataSetAnnotationTag field-value rows (a tag is a field whose
/// value is a type). Required so the configuration write path's cascade can persist each tag.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "DataSetAnnotationTag")]
public sealed class DataSetAnnotationTagConfigurationCommand : ConfigurationCommandBase<DataSetAnnotationTagConfiguration>
{
    /// <inheritdoc/>
    public DataSetAnnotationTagConfigurationCommand() : base("DataSetAnnotationTag") { }
}
