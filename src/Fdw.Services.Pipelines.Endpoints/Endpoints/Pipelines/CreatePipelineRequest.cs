using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request for creating a new pipeline.
/// </summary>
public class CreatePipelineRequest
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline type (e.g., BatchCopy, Streaming).
    /// </summary>
    /// <remarks>
    /// Why: no default engine — a literal default here previously defeated
    /// <c>CreatePipelineRequestValidator</c>'s <c>PipelineType.NotEmpty()</c> rule, since an omitted
    /// value was never actually empty. A caller that omits PipelineType must be rejected, not silently
    /// routed to "BatchCopy".
    /// </remarks>
    [Required]
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source connection name.
    /// </summary>
    [Required]
    public string SourceConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination connection name.
    /// </summary>
    [Required]
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
    /// Gets or sets whether the BatchCopy load truncates the destination before inserting. Use for
    /// snapshot-replace ingests (e.g. "current earthquakes") so a repeating/scheduled run re-loads the
    /// latest snapshot instead of failing a unique index on already-loaded keys. Default false (append).
    /// </summary>
    public bool TruncateBeforeLoad { get; set; }

    /// <summary>
    /// Gets or sets the optional transform (operation) steps for this pipeline. Each entry persists as a
    /// pipe.PipelineOperation row; its <see cref="CreatePipelineTransformRequest.FieldMappings"/> persist
    /// as child pipe.PipelineTransformFieldMapping rows. Maps onto the typed body's
    /// <c>EtlPipelineConfiguration.Transforms</c> collection consumed by the Map transform at runtime.
    /// </summary>
    public IList<CreatePipelineTransformRequest> Transforms { get; set; } = [];
}
