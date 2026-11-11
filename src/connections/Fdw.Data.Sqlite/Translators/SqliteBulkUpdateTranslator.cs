using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Updates;
using Fdw.Data.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.Sqlite;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates <c>BulkUpdateCommand</c> to set-based SQLite statements (FDW-4953): the rows are a <c>VALUES</c> set and
/// <c>WITH s(cols) AS (VALUES ...) UPDATE target AS t SET ... FROM s WHERE keys</c> updates every matched row at once. A batch
/// with more values than one statement may bind is split into as few statements as the parameter limit allows, all in one command inside a savepoint
/// (<see cref="SqliteDataCommandTranslatorBase.ValueSets"/>, <see cref="SqliteDataCommandTranslatorBase.InSavepoint"/>).
/// </summary>
/// <remarks>
/// A key two rows share is collapsed to the last of them (<see cref="SetBasedRows.LastPerKey"/>). A soft delete is refused, as the
/// single-row update refuses it on this kind. The rows affected are the target rows updated.
/// </remarks>
[TypeOption(typeof(SqliteDataCommandTranslators), "BulkUpdate", RestrictToCurrentCompilation = true)]
public sealed class SqliteBulkUpdateTranslator : SqliteDataCommandTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="SqliteBulkUpdateTranslator"/> class.</summary>
    public SqliteBulkUpdateTranslator()
        : base("BulkUpdate")
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
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "BulkUpdate", ex.Message);
            return Task.FromResult(GenericResult<SqliteCommand>.Failure(
                SqliteDataResultCodes.ByName("UpdateTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<SqliteCommand> TranslateCommand(IDataCommand command, IStorageContainer? container)
    {
        if (container is null)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull"));
        if (container.Path is not IDatabasePath path)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath"));
        if (command is not IBulkUpdateCommand bulk)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidCommandType"));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("MissingInputData"), ResultDetails.Create("CommandType", command.CommandType));
        if (data is not IEnumerable rows)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidDataType"), ResultDetails.Create("ActualType", data.GetType().Name));

        var problem = SetBasedColumns.Problem(bulk.Options, container);
        if (problem is not null)
            return GenericResult<SqliteCommand>.Failure(problem);

        RefuseSoftDeleteOnUpdate(command, container);
        var sample = SetBasedUnmatchedKeys.SampleOf(command);
        return sample.IsFailure
            ? sample.ToNewResult<SqliteCommand>()
            : GenericResult<SqliteCommand>.Success(Statements(bulk.Options, rows, container, path, sample.Value));
    }

    private static SqliteCommand Statements(BulkUpdateOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path, int? sample)
    {
        var dialect = path.Dialect;
        var keys = SetBasedColumns.Declared(container, options.KeyColumns);
        var sets = SetBasedColumns.Declared(container, options.SetColumns);
        var staged = SetBasedColumns.Declared(container, options.StagedColumns);
        var batch = SetBasedRows.LastPerKey(rows, keys, out _);

        var target = BuildQualifiedTableName(path);
        var assignments = string.Join(", ", sets.Select(column => $"{dialect.QuoteIdentifier(column)} = s.{dialect.QuoteIdentifier(column)}"));
        var on = string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"));
        var command = CreateCommand(string.Empty);
        command.CommandText = sample is { } size
            ? ReportingUnmatched("fdw_bulk_update", command, batch, staged, keys, size, target, container, dialect, stage => $"UPDATE {target} AS t SET {assignments} FROM {stage} AS s WHERE {on};")
            : InSavepoint(
                "fdw_bulk_update",
                ValueSets(command, batch, staged, container, dialect).Select(values => $"{values}UPDATE {target} AS t SET {assignments} FROM s WHERE {on};"));
        return command;
    }
}
