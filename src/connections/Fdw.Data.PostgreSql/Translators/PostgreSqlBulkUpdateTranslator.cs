using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Updates;
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
/// Translates <c>BulkUpdateCommand</c> to one set-based PostgreSQL statement (FDW-4953):
/// <c>UPDATE target AS t SET ... FROM json_populate_recordset(NULL::target, @fdw_rows) AS s WHERE keys</c>. The rows travel as one
/// JSON parameter and the target's own row type types every value, so a batch is one statement and one round trip.
/// </summary>
/// <remarks>
/// A key two rows share is collapsed to the last of them (<see cref="SetBasedRows.LastPerKey"/>). A soft delete is refused, as the
/// single-row update refuses it on this kind. The rows affected are the target rows updated.
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "BulkUpdate", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlBulkUpdateTranslator : PostgreSqlDataCommandTranslatorBase
{
    private const string RowsParameter = "fdw_rows";

    /// <summary>Initializes a new instance of the <see cref="PostgreSqlBulkUpdateTranslator"/> class.</summary>
    public PostgreSqlBulkUpdateTranslator()
        : base("BulkUpdate")
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
                PostgreSqlDataResultCodes.ByName("UpdateTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<NpgsqlCommand> TranslateCommand(IDataCommand command, IStorageContainer? container)
    {
        if (container is null)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull"));
        if (container.Path is not IDatabasePath path)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath"));
        if (command is not IBulkUpdateCommand bulk)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlTranslatorLog.InvalidCommandType(
                NullLogger<PostgreSqlBulkUpdateTranslator>.Instance, nameof(PostgreSqlBulkUpdateTranslator), nameof(IBulkUpdateCommand), command.GetType().Name));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("MissingInputData"), ResultDetails.Create("CommandType", command.CommandType));
        if (data is not IEnumerable rows)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidDataType"), ResultDetails.Create("ActualType", data.GetType().Name));

        var problem = SetBasedColumns.Problem(bulk.Options, container);
        if (problem is not null)
            return GenericResult<NpgsqlCommand>.Failure(problem);

        RefuseSoftDeleteOnUpdate(command, container);
        var sample = SetBasedUnmatchedKeys.SampleOf(command);
        return sample.IsFailure
            ? sample.ToNewResult<NpgsqlCommand>()
            : GenericResult<NpgsqlCommand>.Success(Statement(bulk.Options, rows, container, path, sample.Value));
    }

    private static NpgsqlCommand Statement(BulkUpdateOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path, int? sample)
    {
        var dialect = path.Dialect;
        var keys = SetBasedColumns.Declared(container, options.KeyColumns);
        var sets = SetBasedColumns.Declared(container, options.SetColumns);
        var staged = SetBasedColumns.Declared(container, options.StagedColumns);

        var batch = SetBasedRows.LastPerKey(rows, keys, out _);
        var json = SetBasedRows.Json(batch, staged, out var problem)
            ?? throw new InvalidOperationException($"The bulk update of container {container.Name} cannot stage its rows: {problem}");

        var target = BuildQualifiedTableName(path);
        var assignments = string.Join(", ", sets.Select(column => $"{dialect.QuoteIdentifier(column)} = s.{dialect.QuoteIdentifier(column)}"));
        var on = string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"));
        var source = $"json_populate_recordset(NULL::{target}, {dialect.ParameterPrefix}{RowsParameter}) AS s";
        var command = CreateCommand(string.Empty);
        command.CommandText = ReportingUnmatched($"UPDATE {target} AS t SET {assignments} FROM {source} WHERE {on}", sample, target, source, keys, dialect, command);
        AddParameter(command, RowsParameter, json, NpgsqlDbType.Json);
        return command;
    }
}
