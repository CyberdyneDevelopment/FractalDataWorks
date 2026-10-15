using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Detailed DTO for a pipeline, including configuration, source/target, and timestamps.
/// </summary>
public class PipelineDetailResponse : IPipelineLoadFields
{
    /// <summary>Gets or sets the pipeline unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the pipeline type (e.g., "BatchCopy", "Streaming").</summary>
    public required string PipelineType { get; set; }

    /// <summary>Gets or sets the source DataSet name.</summary>
    public string? SourceDataSet { get; set; }

    /// <summary>Gets or sets the data store the pipeline reads from, when its source is a data store container (FDW-1103).</summary>
    public string? SourceDataStoreName { get; set; }

    /// <summary>Gets or sets the path within that data store.</summary>
    public string? SourceDataStorePath { get; set; }

    /// <summary>Gets or sets the container within that path.</summary>
    public string? SourceContainerName { get; set; }

    /// <summary>Gets or sets the destination DataSet name.</summary>
    public string? DestinationDataSet { get; set; }

    /// <summary>Gets or sets the data store the pipeline writes to, when its destination is a data store container (FDW-1269).</summary>
    public string? DestinationDataStoreName { get; set; }

    /// <summary>Gets or sets the path within that data store.</summary>
    public string? DestinationDataStorePath { get; set; }

    /// <summary>Gets or sets the container within that path.</summary>
    public string? DestinationContainerName { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the transform steps configured for this pipeline.</summary>
    public IList<PipelineTransformDto> Transforms { get; set; } = [];

    /// <summary>Gets or sets the load mode name (Append, Replace or Upsert) of the implicit load; required when there are no load steps, refused when there are.</summary>
    public string? LoadMode { get; set; }

    /// <summary>Gets or sets the options of the implicit load; required for Upsert, refused for Append and Replace and when there are load steps.</summary>
    public PipelineLoadOptions? LoadOptions { get; set; }

    /// <summary>Gets or sets the ordered load steps, which replace the single implicit load and run in one unit of work.</summary>
    public IList<PipelineLoadStep>? LoadSteps { get; set; }

    /// <summary>
    /// Gets or sets the reject steps (FDW-1142): load steps that write the records the pipeline's stages rejected, after its
    /// load, in a unit of work of their own. A BatchCopy pipeline only. Patched alone: null keeps the stored ones, an empty
    /// list removes them, a list replaces them; never part of the load unit.
    /// </summary>
    public IList<PipelineLoadStep>? RejectSteps { get; set; }

    /// <summary>Gets or sets the value sources of the implicit load; refused when there are load steps.</summary>
    public IList<PipelineLoadColumnSource>? ValueSources { get; set; }

    /// <summary>Gets or sets the declared run parameters.</summary>
    public IList<PipelineParameterDeclaration>? Parameters { get; set; }

    /// <summary>Gets or sets whether the load truncates the destination first: derived from the load mode, true exactly when it is Replace.</summary>
    public bool TruncateBeforeLoad { get; set; }
}
