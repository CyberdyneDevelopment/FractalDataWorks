using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Updates;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Translates <c>BulkUpdateCommand</c> to one set-based T-SQL batch (FDW-4953): stage the rows into a temporary table typed
/// like the target, then one <c>UPDATE t SET ... FROM target t JOIN stage s ON keys</c>. The Modify* audit columns are
/// restamped as the single-row update does, and a marker soft delete flags the rows its marker selects in the same statement.
/// </summary>
/// <remarks>
/// A key two rows share is collapsed to the last of them (<see cref="SetBasedRows.LastPerKey"/>): a join of two source rows
/// to one target row would apply either. The rows affected are the target rows updated.
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "BulkUpdate", RestrictToCurrentCompilation = true)]
public sealed class MsSqlBulkUpdateTranslator : MsSqlDataCommandTranslatorBase, IMsSqlStagingTranslator
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlBulkUpdateTranslator"/> class.</summary>
    public MsSqlBulkUpdateTranslator()
        : base("BulkUpdate")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<MsSqlBulkUpdateTranslator>.Instance;
        if (container is null)
            return Task.FromResult(GenericResult<SqlCommand>.Failure(MsSqlUpsertTranslatorLog.ContainerNull(logger, nameof(MsSqlBulkUpdateTranslator))));

        try
        {
            return Task.FromResult(Inline(Batch(command, container), container));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<SqlCommand>.Failure(
                MsSqlBulkUpdateTranslatorLog.TranslationFailed(logger, ex, container.Name, ex.Message)));
        }
    }

    /// <inheritdoc/>
    /// <remarks>The same write as the inline translation, with the rows bulk copied into the temporary table instead of sent as JSON (FDW-4958).</remarks>
    public IGenericResult<MsSqlStagedWrite> Stage(IDataCommand command, IStorageContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        try
        {
            var batch = Batch(command, container);
            return batch.IsFailure ? batch.ToNewResult<MsSqlStagedWrite>() : batch.Value!.AsStaged(container);
        }
        catch (Exception ex)
        {
            return GenericResult<MsSqlStagedWrite>.Failure(
                MsSqlBulkUpdateTranslatorLog.TranslationFailed(NullLogger<MsSqlBulkUpdateTranslator>.Instance, ex, container.Name, ex.Message));
        }
    }

    private static IGenericResult<SqlCommand> Inline(IGenericResult<MsSqlSetBasedBatch> batch, IStorageContainer container)
    {
        if (batch.IsFailure)
            return batch.ToNewResult<SqlCommand>();

        var command = batch.Value!.Inline(out var problem);
        return command is null
            ? GenericResult<SqlCommand>.Failure(MsSqlBulkUpdateTranslatorLog.Refused(NullLogger<MsSqlBulkUpdateTranslator>.Instance, container.Name, problem ?? string.Empty))
            : GenericResult<SqlCommand>.Success(command);
    }

    private static IGenericResult<MsSqlSetBasedBatch> Batch(IDataCommand command, IStorageContainer container)
    {
        var logger = NullLogger<MsSqlBulkUpdateTranslator>.Instance;
        if (container.Path is not IDatabasePath path)
            return GenericResult<MsSqlSetBasedBatch>.Failure(MsSqlUpsertTranslatorLog.InvalidContainerPath(logger, nameof(MsSqlBulkUpdateTranslator), container.Name));

        if (command is not IBulkUpdateCommand bulk)
            return GenericResult<MsSqlSetBasedBatch>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(logger, nameof(MsSqlBulkUpdateTranslator), nameof(IBulkUpdateCommand), command.GetType().Name));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<MsSqlSetBasedBatch>.Failure(MsSqlUpsertTranslatorLog.MissingInputData(logger, nameof(MsSqlBulkUpdateTranslator), container.Name));
        if (data is not IEnumerable rows)
            return GenericResult<MsSqlSetBasedBatch>.Failure(
                MsSqlUpsertTranslatorLog.InvalidDataType(logger, nameof(MsSqlBulkUpdateTranslator), container.Name, data.GetType().Name));

        var problem = SetBasedColumns.Problem(bulk.Options, container);
        if (problem is not null)
            return GenericResult<MsSqlSetBasedBatch>.Failure(problem);

        var unsafeName = MsSqlLoadColumns.UnknownOrUnsafe(container, bulk.Options.StagedColumns);
        if (unsafeName is not null)
            return GenericResult<MsSqlSetBasedBatch>.Failure(MsSqlBulkUpdateTranslatorLog.Refused(logger, container.Name, unsafeName));

        var soft = MsSqlSoftDeleteSql.For(command, container, path.Dialect, allowAbsentSet: false);
        if (soft.Problem is not null)
            return MsSqlSoftDeleteSql.Refusal(soft).ToNewResult<MsSqlSetBasedBatch>();

        var sample = SetBasedUnmatchedKeys.SampleOf(command);
        return sample.IsFailure ? sample.ToNewResult<MsSqlSetBasedBatch>() : Statement(bulk.Options, rows, container, path, soft, sample.Value);
    }

    private static IGenericResult<MsSqlSetBasedBatch> Statement(
        BulkUpdateOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path, MsSqlSoftDeleteFragments soft, int? sample)
    {
        var logger = NullLogger<MsSqlBulkUpdateTranslator>.Instance;
        var keys = SetBasedColumns.Declared(container, options.KeyColumns);
        var sets = SetBasedColumns.Declared(container, options.SetColumns).Where(column => !MsSqlModifyAuditColumns.Is(column)).ToList();
        var staged = SetBasedColumns.Declared(container, options.StagedColumns);

        var batch = SetBasedRows.LastPerKey(rows, keys, out var collapsed);
        MsSqlBulkUpdateTranslatorLog.Translating(logger, container.Name, batch.Count);

        // A value a date column cannot take is refused here, with its own code, not left for SQL Server converting the staged rows (FDW-4726).
        var refused = MsSqlDateValues.Refusal(container, staged, batch);
        if (refused is not null)
            return refused.ToNewResult<MsSqlSetBasedBatch>();

        var dialect = path.Dialect;
        var target = BuildQualifiedTableName(path);
        var temp = MsSqlRowStaging.NewTempTableName();
        var assignments = sets.Select(column => $"t.{dialect.QuoteIdentifier(column)} = s.{dialect.QuoteIdentifier(column)}")
            .Concat(MsSqlModifyAuditColumns.Declared(container).Select(audit => $"t.{dialect.QuoteIdentifier(audit.Column)} = {audit.Expression}"))
            .Concat(soft.SetClauses("t", onlyWhenMarked: true));
        var on = string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"));

        var command = CreateCommand(string.Empty);
        command.Parameters.AddRange(soft.Parameters.ToArray());
        MsSqlBulkUpdateTranslatorLog.Translated(logger, container.Name, batch.Count, keys.Count, sets.Count, collapsed);
        return GenericResult<MsSqlSetBasedBatch>.Success(new MsSqlSetBasedBatch(
            command, temp, target, dialect, staged, keys, batch,
            string.Empty,
            MsSqlUnmatchedKeys.Around($"UPDATE t SET {string.Join(", ", assignments)} FROM {target} AS t INNER JOIN {temp} AS s ON {on}; ", sample, target, temp, keys, dialect, command),
            UseTransaction: true,
            IdentityInsert: staged.Any(column => MsSqlLoadColumns.IsIdentity(container, column))));
    }
}
