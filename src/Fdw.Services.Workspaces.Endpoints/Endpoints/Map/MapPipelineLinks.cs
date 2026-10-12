using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a pipeline's edges are drawn from: the data sets it names, by name, as the pipeline configuration holds them.</summary>
/// <param name="Id">The pipeline's logical id.</param>
/// <param name="SourceDataSetName">The data set it reads, or nothing when it names none.</param>
/// <param name="DestinationDataSetName">The data set it writes, or nothing when it names none.</param>
internal sealed record MapPipelineLinks(Guid Id, string? SourceDataSetName, string? DestinationDataSetName);
