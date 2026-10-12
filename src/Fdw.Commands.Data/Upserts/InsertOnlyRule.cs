namespace Fdw.Commands.Data.Upserts;

/// <summary>
/// A rule that leaves existing rows alone: a row whose <see cref="Column"/> equals <see cref="Value"/> is never
/// updated or closed by an upsert; only keys that do not exist yet are inserted.
/// </summary>
public sealed class InsertOnlyRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertOnlyRule"/> class.
    /// </summary>
    /// <param name="column">The marker column of the existing rows.</param>
    /// <param name="value">The marker value that protects a row.</param>
    public InsertOnlyRule(string column, object value)
    {
        Column = column;
        Value = value;
    }

    /// <summary>Gets the marker column of the existing rows.</summary>
    public string Column { get; }

    /// <summary>Gets the marker value that protects a row from being changed.</summary>
    public object Value { get; }
}
