using System;

namespace Fdw.Services.Pipelines.Abstractions.Output;

/// <summary>
/// Specifies an individual output column with optional aliasing and transformation.
/// </summary>
public sealed class OutputColumn : IEquatable<OutputColumn>
{
    /// <summary>
    /// Gets or sets the source column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output alias (if different from source name).
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Gets or sets the output data type (for explicit casting).
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Gets or sets whether this column is required in the output.
    /// If true and column is missing, pipeline fails.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Creates a deep copy of this column.
    /// </summary>
    public OutputColumn Clone()
    {
        return new OutputColumn
        {
            Name = Name,
            Alias = Alias,
            DataType = DataType,
            IsRequired = IsRequired
        };
    }

    /// <inheritdoc />
    public bool Equals(OutputColumn? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(Alias, other.Alias, StringComparison.Ordinal) &&
               string.Equals(DataType, other.DataType, StringComparison.Ordinal) &&
               IsRequired == other.IsRequired;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as OutputColumn);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Alias ?? string.Empty);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(DataType ?? string.Empty);
            hash = hash * 31 + IsRequired.GetHashCode();
            return hash;
        }
    }
}
