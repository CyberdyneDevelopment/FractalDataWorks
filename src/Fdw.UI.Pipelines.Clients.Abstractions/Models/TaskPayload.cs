using System;
using System.Collections.Generic;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Represents a task node in the pipeline visual graph.
/// </summary>
public sealed class TaskPayload
{
    /// <summary>
    /// Gets or sets the unique task identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the task name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the task type key (e.g. "SqlQuery", "Filter", "Map").
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the X position on the canvas.
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// Gets or sets the Y position on the canvas.
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// Gets or sets the task-specific configuration dictionary.
    /// </summary>
    public IDictionary<string, object?> Configuration { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
