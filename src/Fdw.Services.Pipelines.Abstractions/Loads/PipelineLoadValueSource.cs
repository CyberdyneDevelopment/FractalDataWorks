using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Where a column's value comes from (<c>source</c>): a differently named source field, a constant, the current
/// timestamp, or the next value of a sequence. Only the fields of the stated <see cref="Kind"/> are used.
/// </summary>
public sealed class PipelineLoadValueSource
{
    /// <summary>Gets or sets the kind name, from the ValueSourceKinds type collection.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the source field (kind Column).</summary>
    public string? SourceField { get; set; }

    /// <summary>Gets or sets the constant's type name, from the DataTypes type collection (kind Constant).</summary>
    public string? ValueType { get; set; }

    /// <summary>Gets or sets the constant's value (kind Constant).</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the sequence name (kind NextSequenceValue).</summary>
    public string? SequenceName { get; set; }

    /// <summary>Gets or sets the columns the sequenced batch is ordered by (kind NextSequenceValue).</summary>
    public IList<string> OrderBy { get; set; } = [];

    /// <summary>Gets or sets the sequence's schema, or <see langword="null"/> when it is not qualified (kind NextSequenceValue).</summary>
    public string? Schema { get; set; }

    /// <summary>Gets or sets the sequence's database, or <see langword="null"/> when it is not qualified (kind NextSequenceValue).</summary>
    public string? Database { get; set; }
}
