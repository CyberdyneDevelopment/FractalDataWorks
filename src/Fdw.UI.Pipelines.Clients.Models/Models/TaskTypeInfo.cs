using System.Collections.Generic;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Describes a task type available in the pipeline designer's task palette.
/// </summary>
public sealed class TaskTypeInfo
{
    /// <summary>
    /// Gets or sets the task type key (used in <see cref="TaskPayload.TaskType"/>).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display category for grouping in the palette (e.g. "Sources", "Transforms").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable description shown in the palette.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of input ports this task type supports.
    /// </summary>
    public int InputPorts { get; set; }

    /// <summary>
    /// Gets or sets the number of output ports this task type supports.
    /// </summary>
    public int OutputPorts { get; set; }

    /// <summary>
    /// Gets or sets the configurable properties for this task type.
    /// </summary>
    public IList<TaskPropertyInfo> Properties { get; set; } = new List<TaskPropertyInfo>();
}
