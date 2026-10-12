namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// Soft-delete options: instead of removing a row, set a configured deleted-flag column (and optionally a
/// deleted-at column). <see cref="Mode"/> picks the rows: those whose marker column equals the marker value, or
/// those whose keys are absent from a source set.
/// </summary>
/// <remarks>
/// The flag is set to true and the deleted-at column, when named, to the database's current timestamp. Rows that are
/// already flagged are left alone.
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

    /// <summary>Gets the optional column that records when a row was flagged, or <see langword="null"/> for none.</summary>
    public string? DeletedAtColumn { get; init; }

    /// <summary>Gets the marker for <c>MarkerMatches</c>; <see langword="null"/> for the other mode.</summary>
    public SoftDeleteMarker? Marker { get; init; }

    /// <summary>Gets the source set for <c>AbsentFromSource</c>; <see langword="null"/> for the other mode.</summary>
    public AbsentKeySet? AbsentFromSource { get; init; }

    /// <summary>Gets a value indicating whether the options flag rows by absence from a source set.</summary>
    public bool HasAbsentSet => ReferenceEquals(Mode, SoftDeleteModes.AbsentFromSource);

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
        if (ReferenceEquals(Mode, SoftDeleteModes.MarkerMatches))
            return MarkerProblem();
        return HasAbsentSet ? AbsentProblem() : $"'{Mode.Name}' is not a soft-delete mode";
    }

    private string? MarkerProblem()
    {
        if (AbsentFromSource is not null)
            return "a marker-matches soft delete takes no source set";
        return Marker is null || string.IsNullOrWhiteSpace(Marker.Column) || Marker.Value is null
            ? "a marker-matches soft delete names a marker column and a marker value"
            : null;
    }

    private string? AbsentProblem()
    {
        if (Marker is not null)
            return "an absent-from-source soft delete takes no marker";
        if (AbsentFromSource is null)
            return "an absent-from-source soft delete names the source set";
        return AbsentFromSource.KeyColumns.Count == 0
            ? "the absent-key set names at least one key column"
            : null;
    }
}
