using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>What a saved pipeline reads and writes, which the roles a run acts with must be able to reach (FDW-1087, FDW-1161).</summary>
/// <param name="PipelineName">The pipeline's name.</param>
/// <param name="SourceDataSet">The data set it reads, if any.</param>
/// <param name="DestinationDataSet">The data set it writes, if any.</param>
/// <param name="SourceConnectionName">The connection it reads from, if any.</param>
/// <param name="DestinationConnectionName">The connection it writes to, if any.</param>
/// <param name="SourceDataStoreName">The data store whose container it reads, if any (FDW-1103).</param>
/// <param name="SourceDataStorePath">The path within that data store.</param>
/// <param name="SourceContainerName">The container within that path.</param>
/// <param name="Steps">What its stages and load steps touch beyond that: Union and Lookup sources, load and reject destinations, Execute containers.</param>
public sealed record PipelineReach(
    string PipelineName,
    string? SourceDataSet,
    string? DestinationDataSet,
    string? SourceConnectionName,
    string? DestinationConnectionName,
    string? SourceDataStoreName,
    string? SourceDataStorePath,
    string? SourceContainerName,
    StepReach Steps)
{
    /// <summary>Reads what a saved pipeline reads and writes from its stored detail.</summary>
    /// <remarks>
    /// The one place a pipeline's reach is read, so triggering it, scheduling it and listing the roles that can run it cannot
    /// answer the question three ways.
    /// </remarks>
    /// <param name="pipeline">The saved pipeline as its detail reads it.</param>
    /// <returns>The pipeline's reach.</returns>
    public static PipelineReach Of(PipelineDetailResponse pipeline) => new(
        pipeline.Name,
        pipeline.SourceDataSet,
        pipeline.DestinationDataSet,
        pipeline.SourceConnectionName,
        pipeline.DestinationConnectionName,
        pipeline.SourceDataStoreName,
        pipeline.SourceDataStorePath,
        pipeline.SourceContainerName,
        StepReach.Of(pipeline));
}
