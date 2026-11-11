using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Deletes;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Translates <c>BulkDeleteCommand</c> to one set-based T-SQL batch (FDW-4954): stage the keys into a temporary table typed like
/// the target's key columns, then one <c>DELETE t FROM target t JOIN stage s ON keys</c>; with a soft delete, one
/// <c>UPDATE t SET flag ... FROM target t JOIN stage s ON keys</c> over the rows its mode selects and that are not flagged yet.
/// </summary>
/// <remarks>The rows affected are the target rows deleted (or flagged).</remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "BulkDelete", RestrictToCurrentCompilation = true)]
public sealed class MsSqlBulkDeleteTranslator : MsSqlDataCommandTranslatorBase, IMsSqlStagingTranslator
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlBulkDeleteTranslator"/> class.</summary>
    public MsSqlBulkDeleteTranslator()
        : base("BulkDelete")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<MsSqlBulkDeleteTranslator>.Instance;
        if (container is null)
            return Task.FromResult(GenericResult<SqlCommand>.Failure(MsSqlUpsertTranslatorLog.ContainerNull(logger, nameof(MsSqlBulkDeleteTranslator))));

        try
        {
            return Task.FromResult(Inline(Batch(command, container), container));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<SqlCommand>.Failure(
                MsSqlBulkDeleteTranslatorLog.TranslationFailed(logger, ex, container.Name, ex.Message)));
        }
    }

    /// <inheritdoc/>
    /// <remarks>The same write as the inline translation, with the keys bulk copied into the temporary table instead of sent as JSON (FDW-4958).</remarks>
    public IGenericResult<MsSqlStagedWrite> Stage(IDataCommand command, IStorageContainer container)
    {
        try
        {
            var batch = Batch(command, container);
            return batch.IsFailure ? batch.ToNewResult<MsSqlStagedWrite>() : batch.Value!.AsStaged(container);
        }
        catch (Exception ex)
        {
            return GenericResult<MsSqlStagedWrite>.Failure(
                MsSqlBulkDeleteTranslatorLog.TranslationFailed(NullLogger<MsSqlBulkDeleteTranslator>.Instance, ex, container.Name, ex.Message));
        }
    }

    private static IGenericResult<SqlCommand> Inline(IGenericResult<MsSqlSetBasedBatch> batch, IStorageContainer container)
    {
        if (batch.IsFailure)
            return batch.ToNewResult<SqlCommand>();

        var command = batch.Value!.Inline(out var problem);
        return command is null
            ? GenericResult<SqlCommand>.Failure(MsSqlBulkDeleteTranslatorLog.Refused(NullLogger<MsSqlBulkDeleteTranslator>.Instance, container.Name, problem ?? string.Empty))
            : GenericResult<SqlCommand>.Success(command);
    }

    private static IGenericResult<MsSqlSetBasedBatch> Batch(IDataCommand command, IStorageContainer container)
    {
        var logger = NullLogger<MsSqlBulkDeleteTranslator>.Instance;
        if (container.Path is not IDatabasePath path)
            return GenericResult<MsSqlSetBasedBatch>.Failure(MsSqlUpsertTranslatorLog.InvalidContainerPath(logger, nameof(MsSqlBulkDeleteTranslator), container.Name));

        if (command is not IBulkDeleteCommand bulk)
            return GenericResult<MsSqlSetBasedBatch>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(logger, nameof(MsSqlBulkDeleteTranslator), nameof(IBulkDeleteCommand), command.GetType().Name));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<MsSqlSetBasedBatch>.Failure(MsSqlUpsertTranslatorLog.MissingInputData(logger, nameof(MsSqlBulkDeleteTranslator), container.Name));
        if (data is not IEnumerable rows)
            return GenericResult<MsSqlSetBasedBatch>.Failure(
                MsSqlUpsertTranslatorLog.InvalidDataType(logger, nameof(MsSqlBulkDeleteTranslator), container.Name, data.GetType().Name));

        var problem = SetBasedColumns.Problem(bulk.Options, container);
        if (problem is not null)
            return GenericResult<MsSqlSetBasedBatch>.Failure(problem);

        var unsafeName = MsSqlLoadColumns.UnknownOrUnsafe(container, bulk.Options.KeyColumns);
        if (unsafeName is not null)
            return GenericResult<MsSqlSetBasedBatch>.Failure(MsSqlBulkDeleteTranslatorLog.Refused(logger, container.Name, unsafeName));

        if (command is ISoftDeletableCommand { SoftDelete.HasAbsentSet: true })
            return GenericResult<MsSqlSetBasedBatch>.Failure(MsSqlBulkDeleteTranslatorLog.Refused(
                logger, container.Name, "flagging the keys absent from a source set is a delete with an absent-key set, not a bulk delete"));

        var soft = MsSqlSoftDeleteSql.For(command, container, path.Dialect, allowAbsentSet: false);
        if (soft.Problem is not null)
            return MsSqlSoftDeleteSql.Refusal(soft).ToNewResult<MsSqlSetBasedBatch>();

        var sample = SetBasedUnmatchedKeys.SampleOf(command);
        return sample.IsFailure ? sample.ToNewResult<MsSqlSetBasedBatch>() : Statement(bulk.Options, rows, container, path, soft, sample.Value);
    }

    private static IGenericResult<MsSqlSetBasedBatch> Statement(
        BulkDeleteOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path, MsSqlSoftDeleteFragments soft, int? sample)
    {
        var logger = NullLogger<MsSqlBulkDeleteTranslator>.Instance;
        var keys = SetBasedColumns.Declared(container, options.KeyColumns);
        var batch = SetBasedRows.LastPerKey(rows, keys, out _);
        MsSqlBulkDeleteTranslatorLog.Translating(logger, container.Name, batch.Count);

        // A value a date key cannot take is refused here, with its own code, not left for SQL Server converting the staged rows (FDW-4726).
        var refused = MsSqlDateValues.Refusal(container, keys, batch);
        if (refused is not null)
            return refused.ToNewResult<MsSqlSetBasedBatch>();

        var dialect = path.Dialect;
        var target = BuildQualifiedTableName(path);
        var temp = MsSqlRowStaging.NewTempTableName();
        var join = $"FROM {target} AS t INNER JOIN {temp} AS s ON {string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"))}";
        var statement = soft.Applies
            ? $"UPDATE t SET {string.Join(", ", soft.SetClauses("t", onlyWhenMarked: false))} {join} WHERE {string.Join(" AND ", new[] { soft.MarkerPredicate("t"), soft.NotYetFlagged("t") }.OfType<string>())}; "
            : $"DELETE t {join}; ";

        var command = CreateCommand(string.Empty);
        command.Parameters.AddRange(soft.Parameters.ToArray());
        MsSqlBulkDeleteTranslatorLog.Translated(logger, soft.Applies ? "soft delete" : "delete", container.Name, batch.Count, keys.Count);
        return GenericResult<MsSqlSetBasedBatch>.Success(new MsSqlSetBasedBatch(
            command, temp, target, dialect, keys, keys, batch, string.Empty, MsSqlUnmatchedKeys.Around(statement, sample, target, temp, keys, dialect, command),
            // Why a transaction when the command reports (FDW-4988): the count is read before the delete under UPDLOCK, HOLDLOCK, which holds only inside one.
            UseTransaction: sample is not null,
            IdentityInsert: keys.Any(key => MsSqlLoadColumns.IsIdentity(container, key))));
    }
}
