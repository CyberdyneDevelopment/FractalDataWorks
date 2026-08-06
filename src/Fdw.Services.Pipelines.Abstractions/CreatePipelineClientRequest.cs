using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request to create a new pipeline.
/// </summary>
public class CreatePipelineClientRequest
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline type (e.g., BatchCopy, Streaming).
    /// </summary>
    /// <remarks>
    /// Why: no default engine — a caller (or a JSON body) that omits this value must be rejected by
    /// the server's <c>PipelineType.NotEmpty()</c> validator, not silently routed to "BatchCopy". A
    /// literal default here previously defeated that validator, since the property was never actually
    /// empty. <c>PipelineCreateRequestProjection.ToCreateRequest</c> fails loud before this DTO is ever
    /// constructed from a canvas with no resolved engine.
    /// </remarks>
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source connection name.
    /// </summary>
    public string SourceConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination connection name.
    /// </summary>
    public string DestinationConnectionName { get; set; } = string.Empty;

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
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional transform (operation) steps for this pipeline. Field names mirror
    /// the server's <c>CreatePipelineRequest.Transforms</c> exactly so the JSON round-trips.
    /// </summary>
    public IList<PipelineTransformClientRequest> Transforms { get; set; } = [];
}
