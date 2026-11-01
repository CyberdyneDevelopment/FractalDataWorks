using System;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Represents a connection between two tasks.
/// </summary>
public sealed class TaskConnectionModel : IEquatable<TaskConnectionModel>
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the source task ID.
    /// </summary>
    public Guid SourceTaskId { get; set; }

    /// <summary>
    /// Gets or sets the source port index.
    /// </summary>
    public int SourcePort { get; set; }

    /// <summary>
    /// Gets or sets the target task ID.
    /// </summary>
    public Guid TargetTaskId { get; set; }

    /// <summary>
    /// Gets or sets the target port index.
    /// </summary>
    public int TargetPort { get; set; }

    /// <summary>
    /// Gets or sets the optional label for the connection.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets whether the connection is valid.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the connection is selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Creates a deep copy of this model.
    /// </summary>
    public TaskConnectionModel Clone()
    {
        return new TaskConnectionModel
        {
            Id = Id,
            SourceTaskId = SourceTaskId,
            SourcePort = SourcePort,
            TargetTaskId = TargetTaskId,
            TargetPort = TargetPort,
            Label = Label,
            IsValid = IsValid,
            IsSelected = false
        };
    }

    /// <inheritdoc />
    public bool Equals(TaskConnectionModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id == other.Id &&
               SourceTaskId == other.SourceTaskId &&
               SourcePort == other.SourcePort &&
               TargetTaskId == other.TargetTaskId &&
               TargetPort == other.TargetPort;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TaskConnectionModel);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Id.GetHashCode();
            hash = hash * 31 + SourceTaskId.GetHashCode();
            hash = hash * 31 + SourcePort;
            hash = hash * 31 + TargetTaskId.GetHashCode();
            hash = hash * 31 + TargetPort;
            return hash;
        }
    }
}
