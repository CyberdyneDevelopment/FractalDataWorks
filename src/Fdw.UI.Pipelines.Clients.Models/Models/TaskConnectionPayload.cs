using System;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Represents a directed connection between two task nodes in a pipeline graph.
/// </summary>
public sealed class TaskConnectionPayload
{
    /// <summary>
    /// Gets or sets the unique connection identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the source task identifier.
    /// </summary>
    public Guid SourceTaskId { get; set; }

    /// <summary>
    /// Gets or sets the source port index.
    /// </summary>
    public int SourcePort { get; set; }

    /// <summary>
    /// Gets or sets the target task identifier.
    /// </summary>
    public Guid TargetTaskId { get; set; }

    /// <summary>
    /// Gets or sets the target port index.
    /// </summary>
    public int TargetPort { get; set; }

    /// <summary>
    /// Gets or sets an optional display label for the connection.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the kind of data flowing over this edge.
    /// Defaults to <c>"data"</c> (normal data stream). Other values: <c>"reject"</c>
    /// (records that failed a filter/validation), <c>"error"</c> (task execution errors).
    /// Reject edges render dashed red in the designer (Wave 0a+).
    /// </summary>
    /// <remarks>
    /// Why string not enum: keeps the surface open-ended so new edge kinds (e.g., "warning")
    /// can be added by consumers without breaking this payload.
    /// </remarks>
    public string EdgeKind { get; set; } = "data";
}
