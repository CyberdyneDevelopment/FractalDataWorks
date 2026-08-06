using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request to update an existing pipeline.
/// </summary>
public class UpdatePipelineClientRequest
{
    /// <summary>
    /// Gets or sets the source connection name.
    /// </summary>
    public string? SourceConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the destination connection name.
    /// </summary>
    public string? DestinationConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the source DataSet name.
    /// </summary>
    public string? SourceDataSet { get; set; }

    /// <summary>
    /// Gets or sets the destination DataSet name.
    /// </summary>
    public string? DestinationDataSet { get; set; }

    /// <summary>
    /// Gets or sets optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the replacement transform (operation) steps for this pipeline. Null means "leave
    /// the existing transforms unchanged" — mirrors the server's <c>UpdatePipelineRequest.Transforms</c>
    /// null semantics exactly.
    /// </summary>
    public IList<PipelineTransformClientRequest>? Transforms { get; set; }
}
