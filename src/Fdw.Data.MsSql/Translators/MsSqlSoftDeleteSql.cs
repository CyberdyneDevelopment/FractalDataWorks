using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Builds the T-SQL for a soft delete: flag a row (set the configured deleted-flag column, and optionally a
/// deleted-at column) instead of removing it. The delete translator uses it for rows a filter matches and for keys
/// absent from a source set; the update translator adds the same assignments to its SET clause.
/// </summary>
internal static class MsSqlSoftDeleteSql
{
    /// <summary>
    /// Reads the soft-delete options off <paramref name="command"/> and checks them against the container.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="container">The target container.</param>
    /// <param name="dialect">The dialect that quotes identifiers.</param>
    /// <param name="allowAbsentSet">Whether an absent-key set is allowed (a delete) or refused (an update).</param>
    internal static MsSqlSoftDeleteFragments For(IDataCommand command, IStorageContainer container, ISqlDialect dialect, bool allowAbsentSet)
    {
        if (command is not ISoftDeletableCommand { SoftDelete: { } options })
            return new MsSqlSoftDeleteFragments();

        var problem = options.FirstProblem() ?? ColumnProblem(options, container);
        if (problem is null && options.HasAbsentSet && !allowAbsentSet)
            problem = "an update flags rows by its filter; the absent-key set belongs to a delete";
        if (problem is not null)
            return new MsSqlSoftDeleteFragments(problem);

        var parameters = new List<SqlParameter> { new("@fdw_sd_flag", true) };
        var marker = options.Marker is null ? null : dialect.QuoteIdentifier(options.Marker.Column);
        if (options.Marker is not null)
            parameters.Add(new SqlParameter("@fdw_sd_marker", options.Marker.Value));
        var deletedAt = options.DeletedAtColumn is null ? null : dialect.QuoteIdentifier(options.DeletedAtColumn);
        return new MsSqlSoftDeleteFragments(options, dialect.QuoteIdentifier(options.DeletedFlagColumn), deletedAt, marker, parameters);
    }

    /// <summary>Builds the failure result for soft-delete options that cannot be used, carrying the reason.</summary>
    /// <param name="soft">The fragments whose <see cref="MsSqlSoftDeleteFragments.Problem"/> is set.</param>
    internal static IGenericResult<SqlCommand> Refusal(MsSqlSoftDeleteFragments soft) =>
        GenericResult<SqlCommand>.Failure(
            MsSqlDataResultCodes.ByName("InvalidSoftDeleteOptions"),
            ResultDetails.Create("Reason", soft.Problem));

    private static string? ColumnProblem(SoftDeleteOptions options, IStorageContainer container)
    {
        var names = new List<string> { options.DeletedFlagColumn };
        if (options.DeletedAtColumn is not null)
            names.Add(options.DeletedAtColumn);
        if (options.Marker is not null)
            names.Add(options.Marker.Column);
        if (options.AbsentFromSource is not null)
            names.AddRange(options.AbsentFromSource.KeyColumns);

        var unknown = MsSqlLoadColumns.UnknownOrUnsafe(container, names);
        if (unknown is not null)
            return unknown;

        var computed = names.FirstOrDefault(n => MsSqlLoadColumns.IsComputed(container, n));
        if (computed is not null)
            return $"column '{computed}' is computed by the database and cannot be a soft-delete column";

        return MsSqlLoadColumns.IsIdentity(container, options.DeletedFlagColumn)
            ? $"column '{options.DeletedFlagColumn}' is an identity column and cannot be the deleted-flag column"
            : null;
    }

    /// <summary>
    /// Builds the soft-delete UPDATE for a delete command: flag the rows <paramref name="whereBuilder"/>'s filter matches
    /// and, when the options carry a source set, the rows whose keys are absent from it.
    /// </summary>
    /// <param name="target">The quoted, qualified target table name.</param>
    /// <param name="dialect">The dialect that quotes identifiers.</param>
    /// <param name="soft">The checked soft-delete options.</param>
    /// <param name="whereBuilder">Adds the filter's parameters to the command and returns its predicate (aliased <c>t</c>), or <see langword="null"/> for no filter.</param>
    /// <param name="problem">Why the source set cannot be staged, or <see langword="null"/>.</param>
    /// <returns>The command, or <see langword="null"/> when <paramref name="problem"/> is set.</returns>
    internal static SqlCommand? BuildDelete(
        string target,
        ISqlDialect dialect,
        MsSqlSoftDeleteFragments soft,
        Func<SqlCommand, string?> whereBuilder,
        out string? problem)
    {
        problem = null;
        var command = new SqlCommand();
        foreach (var parameter in soft.Parameters)
            command.Parameters.Add(parameter);

        var predicates = new List<string>();
        var filter = whereBuilder(command);
        if (filter is not null)
            predicates.Add($"({filter})");
        var marked = soft.MarkerPredicate("t");
        if (marked is not null)
            predicates.Add(marked);
        predicates.Add(soft.NotYetFlagged("t"));

        var update = $"UPDATE t SET {string.Join(", ", soft.SetClauses("t", onlyWhenMarked: false))} FROM {target} AS t WHERE ";
        var absent = soft.Options?.AbsentFromSource;
        if (absent is null)
        {
            command.CommandText = update + string.Join(" AND ", predicates) + ";";
            return command;
        }

        var json = MsSqlRowStaging.Serialize(absent.Rows, absent.KeyColumns, out problem);
        if (json is null)
            return null;

        var temp = MsSqlRowStaging.NewTempTableName();
        var keys = string.Join(" AND ", absent.KeyColumns.Select(k => $"k.{dialect.QuoteIdentifier(k)} = t.{dialect.QuoteIdentifier(k)}"));
        predicates.Add($"NOT EXISTS (SELECT 1 FROM {temp} AS k WHERE {keys})");
        var staging = MsSqlRowStaging.Sql(temp, target, absent.KeyColumns, dialect, identityInsert: false);
        command.CommandText = MsSqlLoadBatch.Wrap(string.Empty, staging + "SET NOCOUNT OFF; " + update + string.Join(" AND ", predicates) + "; ", temp, useTransaction: false);
        command.Parameters.Add(new SqlParameter(MsSqlRowStaging.RowsParameter, System.Data.SqlDbType.NVarChar, -1) { Value = json });
        return command;
    }
}
