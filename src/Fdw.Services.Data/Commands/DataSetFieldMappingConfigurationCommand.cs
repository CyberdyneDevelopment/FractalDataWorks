using Fdw.Collections.Attributes;
using Fdw.Data.DataSets;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the DataSet source's field mapping child.</summary>
/// <remarks>
/// Why this is what was missing: a source can hold its mappings and the schema declares the table,
/// its keys and the foreign key to the source — but the cascade writes each child through the
/// command registered for its container, and there was none for this one. So saving a data set
/// walked past its mappings and wrote nothing, which read as a save that succeeded.
///
/// The translator behind Create resolves the physical DataSetSourceRowId by subquery from the
/// logical DataSetSourceId, so a caller never supplies a row key — which is the reason writing
/// these rows directly could not work.
/// </remarks>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "DataSetFieldMapping")]
public sealed class DataSetFieldMappingConfigurationCommand : ConfigurationCommandBase<DataSetFieldMappingConfiguration>
{
    /// <inheritdoc/>
    public DataSetFieldMappingConfigurationCommand() : base("DataSetFieldMapping") { }
}
