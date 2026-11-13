using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Data.Commands;

/// <summary>ConfigurationCommands TypeOption for the Graph data set's field note child configuration (FDW-5294).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "GraphDataSetFieldNote")]
public sealed class GraphDataSetFieldNoteConfigurationCommand : ConfigurationCommandBase<GraphDataSetFieldNoteConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldNoteConfigurationCommand"/> class.</summary>
    public GraphDataSetFieldNoteConfigurationCommand() : base("GraphDataSetFieldNote") { }
}
