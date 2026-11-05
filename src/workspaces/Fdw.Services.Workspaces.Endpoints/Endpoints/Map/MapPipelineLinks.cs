using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a pipeline's edges are drawn from: the data sets it names, by name, as the pipeline configuration holds them.</summary>
/// <param name="Id">The pipeline's logical id.</param>
/// <param name="SourceDataSetNames">The data sets it reads: none when it names none, several for a graph with several sources.</param>
/// <param name="DestinationDataSetNames">The data sets it writes: none when it names none, several for a graph with several destinations.</param>
/// <param name="SourceContainers">The containers it reads directly, with no data set between (FDW-3824).</param>
/// <param name="DestinationContainers">The containers it writes directly, with no data set between (FDW-3824).</param>
internal sealed record MapPipelineLinks(
    Guid Id,
    IReadOnlyList<string> SourceDataSetNames,
    IReadOnlyList<string> DestinationDataSetNames,
    IReadOnlyList<PipelineContainerRef> SourceContainers,
    IReadOnlyList<PipelineContainerRef> DestinationContainers);
