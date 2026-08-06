using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Edit model for a pipeline definition.
/// </summary>
public sealed class PipelineEditModel : IEquatable<PipelineEditModel>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the pipeline status.
    /// </summary>
    public IPipelineStatus Status { get; set; } = PipelineStatuses.Draft;

    /// <summary>
    /// Gets the tasks in this pipeline.
    /// </summary>
    public IList<TaskEditModel> Tasks { get; set; } = new List<TaskEditModel>();

    /// <summary>
    /// Gets the connections between tasks.
    /// </summary>
    public IList<TaskConnectionModel> Connections { get; set; } = new List<TaskConnectionModel>();

    /// <summary>
    /// Gets or sets when the pipeline was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Creates a deep copy of this model.
    /// </summary>
    public PipelineEditModel Clone()
    {
        return new PipelineEditModel
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Status = Status,
            Tasks = Tasks.Select(t => t.Clone()).ToList(),
            Connections = Connections.Select(c => c.Clone()).ToList(),
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt
        };
    }

    /// <inheritdoc />
    public bool Equals(PipelineEditModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id == other.Id &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(Description, other.Description, StringComparison.Ordinal) &&
               Status.Id == other.Status.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PipelineEditModel);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Id.GetHashCode();
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Description ?? string.Empty);
            hash = hash * 31 + Status.Id.GetHashCode();
            return hash;
        }
    }
}
