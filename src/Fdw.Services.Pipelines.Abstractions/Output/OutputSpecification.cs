using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Abstractions.Output;

/// <summary>
/// Specifies the output columns for a pipeline or task.
/// Columns not in the output specification are automatically discarded at pipeline completion.
/// </summary>
public sealed class OutputSpecification : IEquatable<OutputSpecification>
{
    /// <summary>
    /// Gets or sets the columns that should be included in the output.
    /// </summary>
    public IList<OutputColumn> Columns { get; set; } = new List<OutputColumn>();

    /// <summary>
    /// Gets or sets whether all columns should be output (ignoring the Columns list).
    /// </summary>
    public bool OutputAllColumns { get; set; }

    /// <summary>
    /// Gets or sets whether to include metadata columns (e.g., _rowId, _timestamp).
    /// </summary>
    public bool IncludeMetadata { get; set; }

    /// <summary>
    /// Creates an output specification that outputs all columns.
    /// </summary>
    public static OutputSpecification All()
    {
        return new OutputSpecification { OutputAllColumns = true };
    }

    /// <summary>
    /// Creates an output specification with specific columns.
    /// </summary>
    public static OutputSpecification WithColumns(params string[] columnNames)
    {
        var spec = new OutputSpecification();
        foreach (var name in columnNames)
        {
            spec.Columns.Add(new OutputColumn { Name = name });
        }
        return spec;
    }

    /// <summary>
    /// Creates a deep copy of this specification.
    /// </summary>
    public OutputSpecification Clone()
    {
        var clone = new OutputSpecification
        {
            OutputAllColumns = OutputAllColumns,
            IncludeMetadata = IncludeMetadata
        };
        foreach (var col in Columns)
        {
            clone.Columns.Add(col.Clone());
        }
        return clone;
    }

    /// <inheritdoc />
    public bool Equals(OutputSpecification? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return OutputAllColumns == other.OutputAllColumns &&
               IncludeMetadata == other.IncludeMetadata &&
               Columns.Count == other.Columns.Count;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as OutputSpecification);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + OutputAllColumns.GetHashCode();
            hash = hash * 31 + IncludeMetadata.GetHashCode();
            hash = hash * 31 + Columns.Count;
            return hash;
        }
    }
}
