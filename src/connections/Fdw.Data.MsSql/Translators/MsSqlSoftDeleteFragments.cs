using System.Collections.Generic;
using Fdw.Commands.Data.SoftDeletes;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// What a soft delete contributes to a statement: the SET assignments that flag a row, the marker test, their
/// parameters, and whether the command asked for a soft delete at all. <see cref="Problem"/> is set when the options cannot be used.
/// </summary>
internal sealed class MsSqlSoftDeleteFragments
{
    private readonly string? _flagColumnQuoted;
    private readonly string? _deletedAtQuoted;
    private readonly string? _markerQuoted;

    internal MsSqlSoftDeleteFragments()
    {
        Parameters = [];
    }

    internal MsSqlSoftDeleteFragments(string problem)
    {
        Problem = problem;
        Parameters = [];
    }

    internal MsSqlSoftDeleteFragments(
        SoftDeleteOptions options,
        string flagColumnQuoted,
        string? deletedAtQuoted,
        string? markerQuoted,
        List<SqlParameter> parameters)
    {
        Options = options;
        _flagColumnQuoted = flagColumnQuoted;
        _deletedAtQuoted = deletedAtQuoted;
        _markerQuoted = markerQuoted;
        Parameters = parameters;
    }

    /// <summary>Gets why the options cannot be used, or <see langword="null"/>.</summary>
    internal string? Problem { get; }

    /// <summary>Gets the options, or <see langword="null"/> when the command asked for no soft delete.</summary>
    internal SoftDeleteOptions? Options { get; }

    /// <summary>Gets a value indicating whether the command asked for a soft delete and the options are usable.</summary>
    internal bool Applies => Options is not null && Problem is null;

    /// <summary>Gets the parameters the SET assignments and the not-yet-flagged test use; empty when not a soft delete.</summary>
    internal List<SqlParameter> Parameters { get; }

    /// <summary>
    /// Returns the SET assignments that flag a row, qualified by <paramref name="qualifier"/> when given; empty when not
    /// a soft delete. With <paramref name="onlyWhenMarked"/> the assignments take effect only on rows the marker selects
    /// (an update that also soft deletes); otherwise the statement's own WHERE has already chosen the rows.
    /// </summary>
    internal IReadOnlyList<string> SetClauses(string? qualifier, bool onlyWhenMarked)
    {
        if (!Applies)
            return [];
        var prefix = qualifier is null ? string.Empty : $"{qualifier}.";
        var clauses = new List<string> { Assign($"{prefix}{_flagColumnQuoted}", "@fdw_sd_flag", prefix, onlyWhenMarked) };
        if (_deletedAtQuoted is not null)
            clauses.Add(Assign($"{prefix}{_deletedAtQuoted}", "SYSDATETIMEOFFSET()", prefix, onlyWhenMarked));
        return clauses;
    }

    /// <summary>Returns the predicate that selects rows by the marker, or <see langword="null"/> when the mode is not marker-matches.</summary>
    /// <param name="alias">The row's alias.</param>
    internal string? MarkerPredicate(string alias) =>
        _markerQuoted is null ? null : $"{alias}.{_markerQuoted} = @fdw_sd_marker";

    private string Assign(string column, string value, string prefix, bool onlyWhenMarked) =>
        onlyWhenMarked && _markerQuoted is not null
            ? $"{column} = CASE WHEN {prefix}{_markerQuoted} = @fdw_sd_marker THEN {value} ELSE {column} END"
            : $"{column} = {value}";

    /// <summary>Returns the predicate that skips rows already flagged, using <paramref name="alias"/> as the row's alias.</summary>
    internal string NotYetFlagged(string alias) =>
        $"({alias}.{_flagColumnQuoted} IS NULL OR {alias}.{_flagColumnQuoted} <> @fdw_sd_flag)";
}
