using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Fdw.Commands.Data.Logging;
using Fdw.Messages;
using Microsoft.Extensions.Logging.Abstractions;

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
            problem = Inconsistent("an update flags rows by its filter; the absent-key set belongs to a delete");
        if (problem is not null)
            return new MsSqlSoftDeleteFragments(problem.Message);

        var parameters = new List<SqlParameter> { new("@fdw_sd_flag", options.FlagValue) };
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

    private static IGenericMessage? ColumnProblem(SoftDeleteOptions options, IStorageContainer container)
    {
        var names = new List<string> { options.DeletedFlagColumn };
        if (options.DeletedAtColumn is not null)
            names.Add(options.DeletedAtColumn);
        if (options.Marker is not null)
            names.Add(options.Marker.Column);
        if (options.AbsentFromSource is not null)
            names.AddRange(options.AbsentFromSource.KeyColumns);
        if (options.AbsentFromStage is not null)
            names.AddRange(options.AbsentFromStage.KeyColumns);

        var unknown = MsSqlLoadColumns.UnknownOrUnsafe(container, names);
        if (unknown is not null)
            return Inconsistent(unknown);

        var stage = options.AbsentFromStage is null ? null : StageProblem(options.AbsentFromStage);
        if (stage is not null)
            return stage;

        var computed = names.FirstOrDefault(n => MsSqlLoadColumns.IsComputed(container, n));
        if (computed is not null)
            return Inconsistent($"column '{computed}' is computed by the database and cannot be a soft-delete column");

        return MsSqlLoadColumns.IsIdentity(container, options.DeletedFlagColumn)
            ? Inconsistent($"column '{options.DeletedFlagColumn}' is an identity column and cannot be the deleted-flag column")
            : null;
    }

    // Why the stage is checked here: the sweep reads the key columns and the stage key column from it, so a stage that is not a table, or that does not declare
    // one of them, is refused by name before anything is sent.
    private static IGenericMessage? StageProblem(StagedKeySet staged)
    {
        if (staged.Stage.Path is not IDatabasePath)
            return Inconsistent($"the stage container {staged.Stage.Name} is not a database table");

        var unknown = MsSqlLoadColumns.UnknownOrUnsafe(staged.Stage, [.. staged.KeyColumns, staged.StageKeyColumn]);
        return unknown is null ? null : Inconsistent($"in the stage container {staged.Stage.Name}: {unknown}");
    }

    // The options' own problems and this container's are the same kind of refusal: the options cannot be used here.
    private static IGenericMessage Inconsistent(string reason)
        => CommandOptionsLog.SoftDeleteOptionsInconsistent(NullLogger.Instance, reason);

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
        if (soft.Options?.AbsentFromStage is { } staged)
        {
            command.CommandText = StagedSweep(staged, dialect, command, update, predicates);
            return command;
        }

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

    // The sweep of a staged key set (FDW-4956), one batch: the stage rows of this load are counted and must be exactly the rows it appended, else the batch
    // raises StageRowCountMismatchError and flags nothing; then the target rows whose keys have no stage row are flagged. A short stage would make present keys
    // look gone, so the count is read in the same batch, inside the caller's transaction.
    private static string StagedSweep(StagedKeySet staged, ISqlDialect dialect, SqlCommand command, string update, List<string> predicates)
    {
        var stage = SqlNames.QualifiedTable((IDatabasePath)staged.Stage.Path);
        var stageKey = dialect.QuoteIdentifier(staged.StageKeyColumn);
        command.Parameters.Add(new SqlParameter("@fdw_stage_key", System.Data.SqlDbType.NVarChar, -1) { Value = staged.StageKey });
        command.Parameters.Add(new SqlParameter("@fdw_stage_expected", System.Data.SqlDbType.BigInt) { Value = staged.ExpectedRows });
        var keys = string.Join(" AND ", staged.KeyColumns.Select(k => $"k.{dialect.QuoteIdentifier(k)} = t.{dialect.QuoteIdentifier(k)}"));
        predicates.Add($"NOT EXISTS (SELECT 1 FROM {stage} AS k WHERE k.{stageKey} = @fdw_stage_key AND {keys})");
        return $"DECLARE @fdw_stage_found bigint = (SELECT COUNT_BIG(*) FROM {stage} WHERE {stageKey} = @fdw_stage_key); "
               + "IF @fdw_stage_found <> @fdw_stage_expected BEGIN "
               + "DECLARE @fdw_stage_message nvarchar(400) = CONCAT(N'The stage holds ', @fdw_stage_found, N' row(s) for this load; it appended ', @fdw_stage_expected); "
               + $"THROW {MsSqlDeleteTranslator.StageRowCountMismatchError}, @fdw_stage_message, 1; END; "
               + update + string.Join(" AND ", predicates) + ";";
    }
}
