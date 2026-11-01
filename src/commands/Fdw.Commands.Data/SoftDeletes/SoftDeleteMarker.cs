namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// The marker that selects rows for a <c>MarkerMatches</c> soft delete: a row whose
/// <see cref="Column"/> equals <see cref="Value"/> is flagged.
/// </summary>
public sealed class SoftDeleteMarker
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteMarker"/> class.
    /// </summary>
    /// <param name="column">The marker column.</param>
    /// <param name="value">The marker value that selects a row.</param>
    public SoftDeleteMarker(string column, object value)
    {
        Column = column;
        Value = value;
    }

    /// <summary>Gets the marker column.</summary>
    public string Column { get; }

    /// <summary>Gets the marker value that selects a row.</summary>
    public object Value { get; }
}
