namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// Soft-delete options: instead of removing a row, set a configured deleted-flag column (and optionally a
/// deleted-at column). <see cref="Mode"/> picks the rows: those whose marker column equals the marker value, those whose keys are
/// absent from a source set, or those the command's own filter matches.
/// </summary>
/// <remarks>
/// The flag is set to <see cref="FlagValue"/> (true unless stated) and the deleted-at column, when named, to the
/// database's current timestamp. Rows already holding that value are left alone. A flag column that marks the live
/// version of a row (IsCurrent) is closed by setting it to false.
/// </remarks>
public sealed class SoftDeleteOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteOptions"/> class.
    /// </summary>
    /// <param name="deletedFlagColumn">The column that marks a row deleted.</param>
    /// <param name="mode">Which rows are flagged.</param>
    public SoftDeleteOptions(string deletedFlagColumn, ISoftDeleteMode mode)
    {
        DeletedFlagColumn = deletedFlagColumn;
        Mode = mode;
    }

    /// <summary>Gets the column that marks a row deleted.</summary>
    public string DeletedFlagColumn { get; }

    /// <summary>Gets which rows are flagged.</summary>
    public ISoftDeleteMode Mode { get; }

    /// <summary>
    /// Gets the value the flag column is set to: true for a deleted flag, false to clear a flag that marks the live
    /// version of a row.
    /// </summary>
    public bool FlagValue { get; init; } = true;

    /// <summary>Gets the optional column that records when a row was flagged, or <see langword="null"/> for none.</summary>
    public string? DeletedAtColumn { get; init; }

    /// <summary>Gets the marker for <c>MarkerMatches</c>; <see langword="null"/> for the other mode.</summary>
    public SoftDeleteMarker? Marker { get; init; }

    /// <summary>Gets the source set for <c>AbsentFromSource</c>; <see langword="null"/> for the other mode.</summary>
    public AbsentKeySet? AbsentFromSource { get; init; }

    /// <summary>Gets a value indicating whether the options flag rows by absence from a source set.</summary>
    public bool HasAbsentSet => Mode.NeedsSourceSet;

    /// <summary>
    /// Describes the first thing wrong with these options, or returns <see langword="null"/> when they are
    /// consistent. Column names are checked against the target's schema by the translator.
    /// </summary>
    /// <returns>A plain-language reason, or <see langword="null"/>.</returns>
    public string? FirstProblem()
    {
        if (string.IsNullOrWhiteSpace(DeletedFlagColumn))
            return "a soft delete names the deleted-flag column";
        if (DeletedAtColumn is not null && string.IsNullOrWhiteSpace(DeletedAtColumn))
            return "a soft delete's deleted-at column is named or left out, not blank";
        return Mode.Problem(this);
    }
}
