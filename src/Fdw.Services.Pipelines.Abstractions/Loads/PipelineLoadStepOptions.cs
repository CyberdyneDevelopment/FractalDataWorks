using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The options of one load step. Which are stated depends on the step's kind (contract v2 item 3): the shared
/// options of <see cref="PipelineLoadOptions"/>, and for InsertFromQuery a source data set and filter, and for
/// Execute a destination container and its parameters.
/// </summary>
public sealed class PipelineLoadStepOptions : PipelineLoadOptions
{
    /// <summary>Gets or sets the data set the rows are read from (InsertFromQuery; on the destination's connection).</summary>
    public string? SourceDataSet { get; set; }

    /// <summary>Gets or sets the filter on the source (InsertFromQuery), which may reference run parameters.</summary>
    public string? FilterExpression { get; set; }

    /// <summary>Gets or sets the container an Execute step runs.</summary>
    public PipelineLoadDestinationContainer? DestinationContainer { get; set; }

    /// <summary>Gets or sets the parameters an Execute step passes to its container.</summary>
    public IList<PipelineLoadExecuteParameter> Parameters { get; set; } = [];
}
