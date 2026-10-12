
namespace Fdw.Commands.Data.Upserts;

/// <summary>
/// Versioned-history options for an upsert: instead of updating a changed row in place, the current row is
/// closed and a new current row is opened.
/// </summary>
/// <remarks>
/// A row is current while its effective-to column is NULL. When a compare column differs, the current row's
/// effective-to is set to the database's current timestamp (and its current-flag column, when named, is cleared)
/// and a new row is inserted whose effective-from is that same timestamp, whose effective-to is NULL and whose
/// current flag is set. The columns are owned by the command: they are not in the write columns. Rows carrying the
/// <see cref="InsertOnlyRule"/> marker are never closed or changed.
/// </remarks>
public sealed class VersioningOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VersioningOptions"/> class.
    /// </summary>
    /// <param name="effectiveFromColumn">The column that holds when a version became current.</param>
    /// <param name="effectiveToColumn">The column that holds when a version stopped being current; NULL while current.</param>
    public VersioningOptions(string effectiveFromColumn, string effectiveToColumn)
    {
        EffectiveFromColumn = effectiveFromColumn;
        EffectiveToColumn = effectiveToColumn;
    }

    /// <summary>Gets the column that holds when a version became current.</summary>
    public string EffectiveFromColumn { get; }

    /// <summary>Gets the column that holds when a version stopped being current; NULL while it is current.</summary>
    public string EffectiveToColumn { get; }

    /// <summary>Gets the optional flag column: set on the current row, cleared when the row is closed; or <see langword="null"/> for none.</summary>
    public string? CurrentFlagColumn { get; init; }

    /// <summary>Gets the rule that leaves rows carrying a marker value untouched, or <see langword="null"/> for none.</summary>
    public InsertOnlyRule? InsertOnlyRule { get; init; }
}
