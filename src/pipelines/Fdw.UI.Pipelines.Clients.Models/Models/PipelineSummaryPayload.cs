using System;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Summary information for a pipeline in the designer.
/// </summary>
public sealed class PipelineSummaryPayload
{
    /// <summary>
    /// Gets or sets the unique pipeline identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the pipeline status.
    /// </summary>
    public IPipelineStatus Status { get; set; } = PipelineStatuses.Draft;

    /// <summary>
    /// Gets or sets when the pipeline was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}
