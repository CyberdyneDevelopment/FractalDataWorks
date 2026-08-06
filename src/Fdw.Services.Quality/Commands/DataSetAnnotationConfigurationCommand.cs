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

    // Why: catalog.DataSetAnnotation keys on DataSetName, not Name — the default "Name" column does not
    // exist there (SQL 207). Get(name) filters by DataSetName so the annotation (and its cascade) loads.
    /// <inheritdoc/>
    protected override string NameColumn => "DataSetName";
}
