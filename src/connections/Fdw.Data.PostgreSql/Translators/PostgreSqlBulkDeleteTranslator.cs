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
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates <c>BulkDeleteCommand</c> to one set-based PostgreSQL statement (FDW-4954):
/// <c>DELETE FROM target AS t USING json_populate_recordset(NULL::target, @fdw_rows) AS s WHERE keys</c>; a soft delete flags the
/// matched rows with the shared soft-delete UPDATE and an <c>EXISTS</c> over the same row set. The keys travel as one JSON parameter
/// typed by the target's row type, so a batch is one statement and one round trip.
/// </summary>
/// <remarks>The rows affected are the target rows deleted (or flagged).</remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "BulkDelete", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlBulkDeleteTranslator : PostgreSqlDataCommandTranslatorBase
{
    private const string RowsParameter = "fdw_rows";

    /// <summary>Initializes a new instance of the <see cref="PostgreSqlBulkDeleteTranslator"/> class.</summary>
    public PostgreSqlBulkDeleteTranslator()
        : base("BulkDelete")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(TranslateCommand(command, container));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlDataResultCodes.ByName("DeleteTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<NpgsqlCommand> TranslateCommand(IDataCommand command, IStorageContainer? container)
    {
        if (container is null)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull"));
        if (container.Path is not IDatabasePath path)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath"));
        if (command is not IBulkDeleteCommand bulk)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlTranslatorLog.InvalidCommandType(
                NullLogger<PostgreSqlBulkDeleteTranslator>.Instance, nameof(PostgreSqlBulkDeleteTranslator), nameof(IBulkDeleteCommand), command.GetType().Name));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("MissingInputData"), ResultDetails.Create("CommandType", command.CommandType));
        if (data is not IEnumerable rows)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidDataType"), ResultDetails.Create("ActualType", data.GetType().Name));

        var problem = SetBasedColumns.Problem(bulk.Options, container);
        if (problem is not null)
            return GenericResult<NpgsqlCommand>.Failure(problem);

        var sample = SetBasedUnmatchedKeys.SampleOf(command);
        return sample.IsFailure
            ? sample.ToNewResult<NpgsqlCommand>()
            : GenericResult<NpgsqlCommand>.Success(Statement(bulk.Options, rows, container, path, (command as ISoftDeletableCommand)?.SoftDelete, sample.Value));
    }

    private static NpgsqlCommand Statement(BulkDeleteOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path, SoftDeleteOptions? soft, int? sample)
    {
        var dialect = path.Dialect;
        var keys = SetBasedColumns.Declared(container, options.KeyColumns);
        var batch = SetBasedRows.LastPerKey(rows, keys, out _);
        var json = SetBasedRows.Json(batch, keys, out var problem)
            ?? throw new InvalidOperationException($"The bulk delete of container {container.Name} cannot stage its keys: {problem}");

        var target = BuildQualifiedTableName(path);
        var source = $"json_populate_recordset(NULL::{target}, {dialect.ParameterPrefix}{RowsParameter}) AS s";
        var on = string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"));
        var command = CreateCommand(string.Empty);
        var write = soft is null
            ? $"DELETE FROM {target} AS t USING {source} WHERE {on}"
            : BuildSoftDeleteUpdate($"{target} AS t", dialect, soft, container, "now()", $"EXISTS (SELECT 1 FROM {source} WHERE {on})", (name, value) => AddParameter(command, name, value));
        command.CommandText = ReportingUnmatched(write, sample, target, source, keys, dialect, command);
        AddParameter(command, RowsParameter, json, NpgsqlDbType.Json);
        return command;
    }
}
