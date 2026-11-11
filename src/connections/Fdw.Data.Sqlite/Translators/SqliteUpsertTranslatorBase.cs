using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Upserts;
using Fdw.Data.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.Sqlite;

namespace Fdw.Data.Sqlite.Translators;

/// <summary>
/// Shared translation of an upsert to set-based SQLite statements (FDW-4955): the rows are a <c>VALUES</c> set, bound once and
/// read by both statements, then the changed rows are updated (or, with versioning, closed) and the new keys inserted
/// (<see cref="SetBasedUpsert"/>). The statements run inside a savepoint so they land together, inside a unit of work's
/// transaction or on their own; a batch beyond the parameter limit is split only there. The upsert and bulk upsert translators
/// differ only in the command name they answer to.
/// </summary>
/// <remarks>
/// A versioned close and its insert share one instant, bound once as <c>@fdw_now</c> in UTC, as SQLite's own CURRENT_TIMESTAMP is.
/// A key two rows share collapses to the last row. The rows affected are the rows updated (or closed) and inserted.
/// </remarks>
public abstract class SqliteUpsertTranslatorBase : SqliteDataCommandTranslatorBase
{
    private const string NowParameter = "fdw_now";
    private const string Savepoint = "fdw_upsert";

    /// <summary>Initializes a new instance of the <see cref="SqliteUpsertTranslatorBase"/> class.</summary>
    /// <param name="name">The command name this translator answers to (must match its <c>[TypeOption]</c>).</param>
    protected SqliteUpsertTranslatorBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqliteCommand>> Translate(
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
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, Name, ex.Message);
            return Task.FromResult(GenericResult<SqliteCommand>.Failure(
                SqliteDataResultCodes.ByName("InsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<SqliteCommand> TranslateCommand(IDataCommand command, IStorageContainer? container)
    {
        if (container is null)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull"));
        if (container.Path is not IDatabasePath path)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath"));
        if (command is not IUpsertCommand upsert)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidCommandType"));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("MissingInputData"), ResultDetails.Create("CommandType", command.CommandType));
        if (data is not IEnumerable rows)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidDataType"), ResultDetails.Create("ActualType", data.GetType().Name));

        var problem = SetBasedUpsert.Problem(upsert.Options, container, "SQLite");
        return problem is not null
            ? GenericResult<SqliteCommand>.Failure(problem)
            : GenericResult<SqliteCommand>.Success(Statements(upsert.Options, rows, container, path));
    }

    private static SqliteCommand Statements(UpsertOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path)
    {
        var dialect = path.Dialect;
        var batch = SetBasedRows.LastPerKey(rows, SetBasedColumns.Declared(container, options.KeyColumns), out _);
        var target = BuildQualifiedTableName(path);
        var command = CreateCommand(string.Empty);
        if (options.Versioning is not null)
            AddParameter(command, NowParameter, DateTime.UtcNow);

        var statements = ValueSets(command, batch, SetBasedUpsert.StagedColumns(options, container), container, dialect)
            .SelectMany(values => SetBasedUpsert.Statements(
                    options,
                    container,
                    dialect,
                    target,
                    "s",
                    (left, right) => $"{left} IS NOT {right}",
                    $"{dialect.ParameterPrefix}{NowParameter}",
                    (name, value) => AddOnce(command, name, value))
                .Where(statement => statement.Length > 0)
                .Select(statement => $"{values}{statement};"));
        command.CommandText = InSavepoint(Savepoint, statements);
        return command;
    }

    // The upsert's own parameters (the marker, the flag values) are the same for every group of rows, so each is added once.
    private static void AddOnce(SqliteCommand command, string name, object? value)
    {
        if (!command.Parameters.Contains($"@{name}"))
            AddParameter(command, name, value);
    }
}
