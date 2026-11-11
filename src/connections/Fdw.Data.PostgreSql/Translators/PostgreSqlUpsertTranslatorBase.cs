using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Upserts;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;

namespace Fdw.Data.PostgreSql.Translators;

/// <summary>
/// Shared translation of an upsert to one PostgreSQL command (FDW-4955): the rows travel as one JSON parameter read by
/// <c>json_populate_recordset(NULL::target, @fdw_rows)</c>, then the changed rows are updated (or, with versioning, closed) and
/// the new keys inserted (<see cref="SetBasedUpsert"/>). PostgreSQL runs the command's statements as one implicit transaction, so
/// they land together and <c>now()</c> is one instant for a close and its insert. The target is locked first (SHARE ROW EXCLUSIVE)
/// so a concurrent load cannot insert the same new key. The upsert and bulk upsert translators differ only
/// in the command name they answer to.
/// </summary>
/// <remarks>A key two rows share collapses to the last row. The rows affected are the rows updated (or closed) and inserted.</remarks>
public abstract class PostgreSqlUpsertTranslatorBase : PostgreSqlDataCommandTranslatorBase
{
    private const string RowsParameter = "fdw_rows";

    /// <summary>Initializes a new instance of the <see cref="PostgreSqlUpsertTranslatorBase"/> class.</summary>
    /// <param name="name">The command name this translator answers to (must match its <c>[TypeOption]</c>).</param>
    protected PostgreSqlUpsertTranslatorBase(string name)
        : base(name)
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
                PostgreSqlDataResultCodes.ByName("InsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private IGenericResult<NpgsqlCommand> TranslateCommand(IDataCommand command, IStorageContainer? container)
    {
        if (container is null)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull"));
        if (container.Path is not IDatabasePath path)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath"));
        if (command is not IUpsertCommand upsert)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlTranslatorLog.InvalidCommandType(
                NullLogger<PostgreSqlUpsertTranslatorBase>.Instance, Name, nameof(IUpsertCommand), command.GetType().Name));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("MissingInputData"), ResultDetails.Create("CommandType", command.CommandType));
        if (data is not IEnumerable rows)
            return GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidDataType"), ResultDetails.Create("ActualType", data.GetType().Name));

        var problem = SetBasedUpsert.Problem(upsert.Options, container, "PostgreSQL");
        return problem is not null
            ? GenericResult<NpgsqlCommand>.Failure(problem)
            : GenericResult<NpgsqlCommand>.Success(Statement(upsert.Options, rows, container, path));
    }

    private static NpgsqlCommand Statement(UpsertOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path)
    {
        var dialect = path.Dialect;
        var staged = SetBasedUpsert.StagedColumns(options, container);
        var batch = SetBasedRows.LastPerKey(rows, SetBasedColumns.Declared(container, options.KeyColumns), out _);
        var json = SetBasedRows.Json(batch, staged, out var problem)
            ?? throw new InvalidOperationException($"The upsert into container {container.Name} cannot stage its rows: {problem}");

        var target = BuildQualifiedTableName(path);
        var command = CreateCommand(string.Empty);
        var statements = SetBasedUpsert.Statements(
            options,
            container,
            dialect,
            target,
            $"json_populate_recordset(NULL::{target}, {dialect.ParameterPrefix}{RowsParameter}) AS s",
            (left, right) => $"{left} IS DISTINCT FROM {right}",
            "now()",
            (name, value) => AddParameter(command, name, value));
        // The target is locked against other writers (readers still read) before the update and insert, so two loads cannot both
        // find a new key missing and both insert it; SQL Server's upsert holds UPDLOCK, HOLDLOCK for the same reason. The lock lasts
        // to the end of the command's implicit transaction, or of the unit of work's.
        command.CommandText = string.Join("; ", statements.Where(statement => statement.Length > 0).Prepend($"LOCK TABLE {target} IN SHARE ROW EXCLUSIVE MODE"));
        AddParameter(command, RowsParameter, json, NpgsqlDbType.Json);
        return command;
    }
}
