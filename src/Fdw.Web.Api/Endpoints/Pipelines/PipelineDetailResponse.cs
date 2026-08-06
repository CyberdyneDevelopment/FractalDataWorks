using System;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Detailed DTO for a pipeline, including configuration, source/target, and timestamps.
/// </summary>
public class PipelineDetailResponse : ResourceDetail
{
    /// <summary>Gets or sets the pipeline type (e.g., "BatchCopy", "Streaming").</summary>
    public required string PipelineType { get; set; }

    /// <summary>Gets or sets the source connection name.</summary>
    public required string SourceConnectionName { get; set; }

    /// <summary>Gets or sets the destination connection name.</summary>
    public required string DestinationConnectionName { get; set; }

    /// <summary>Gets or sets the source DataSet name.</summary>
    public string? SourceDataSet { get; set; }

    /// <summary>Gets or sets the destination DataSet name.</summary>
    public string? DestinationDataSet { get; set; }

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
}
