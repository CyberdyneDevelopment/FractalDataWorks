using System;

namespace Fdw.Commands.Data.ValueSources;

/// <summary>
/// A column value read from a field of the source rows: the destination column takes the value of the named
/// source field, which need not be called the same.
/// </summary>
/// <remarks>
/// It is meaningful where a command reads from a source query (<see cref="Fdw.Commands.Data.InsertFromQueryCommand"/>). A
/// command that is handed rows already keyed by destination column refuses it.
/// </remarks>
public sealed class ColumnReferenceValueSource : ColumnValueSource
{
    /// <summary>The kind name connection types render.</summary>
    public const string KindName = "Column";

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnReferenceValueSource"/> class.
    /// </summary>
    /// <param name="sourceField">The source column or field that supplies the value.</param>
    public ColumnReferenceValueSource(string sourceField)
        : base(KindName)
    {
        SourceField = string.IsNullOrWhiteSpace(sourceField)
            ? throw new ArgumentException("A column value source names its source field.", nameof(sourceField))
            : sourceField;
    }

    /// <summary>Gets the source column or field that supplies the value.</summary>
    public string SourceField { get; }
}
