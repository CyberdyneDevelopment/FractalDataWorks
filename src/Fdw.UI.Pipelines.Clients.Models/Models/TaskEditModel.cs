using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Abstractions.DataSource;
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.Output;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Edit model for a task within a pipeline.
/// </summary>
public sealed class TaskEditModel : IEquatable<TaskEditModel>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the task type (SqlQuery, Filter, Map, etc.).
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the task name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position on the canvas.
    /// </summary>
    public Point Position { get; set; } = Point.Zero;

    /// <summary>
    /// Gets or sets the task configuration.
    /// </summary>
    public IDictionary<string, object?> Configuration { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the primary data source for this task.
    /// </summary>
    public DataSourceReference? Source { get; set; }

    /// <summary>
    /// Gets or sets the data destination for this task.
    /// </summary>
    public DataDestinationReference? Destination { get; set; }

    /// <summary>
    /// Gets or sets additional data sources for tasks that require multiple inputs.
    /// </summary>
    public IList<DataSourceReference>? AdditionalSources { get; set; }

    /// <summary>
    /// Gets or sets the column disposal specification.
    /// </summary>
    public ColumnDisposal? ColumnDisposal { get; set; }

    /// <summary>
    /// Gets or sets the output specification.
    /// </summary>
    public OutputSpecification? Output { get; set; }

    /// <summary>
    /// Gets the validation errors for this task.
    /// </summary>
    public IList<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether the task is valid.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the task is selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets the number of input ports.
    /// </summary>
    public int InputPorts { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of output ports.
    /// </summary>
    public int OutputPorts { get; set; } = 1;

    /// <summary>
    /// Creates a deep copy of this model.
    /// </summary>
    public TaskEditModel Clone()
    {
        return new TaskEditModel
        {
            Id = Id,
            TaskType = TaskType,
            Name = Name,
            Position = Position,
            Configuration = new Dictionary<string, object?>(Configuration, StringComparer.Ordinal),
            Source = Source?.Clone(),
            Destination = Destination?.Clone(),
            AdditionalSources = AdditionalSources?.Select(s => s.Clone()).ToList(),
            ColumnDisposal = ColumnDisposal?.Clone(),
            Output = Output?.Clone(),
            Errors = new List<string>(Errors),
            IsValid = IsValid,
            IsSelected = false,
            InputPorts = InputPorts,
            OutputPorts = OutputPorts
        };
    }

    /// <inheritdoc />
    public bool Equals(TaskEditModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id == other.Id &&
               string.Equals(TaskType, other.TaskType, StringComparison.Ordinal) &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               Position == other.Position;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TaskEditModel);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Id.GetHashCode();
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(TaskType ?? string.Empty);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
            hash = hash * 31 + Position.GetHashCode();
            return hash;
        }
    }
}
