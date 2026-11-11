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
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.Sqlite;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates <c>BulkDeleteCommand</c> to set-based SQLite statements (FDW-4954): the keys are a <c>VALUES</c> set and
/// <c>WITH s(keys) AS (VALUES ...) DELETE FROM target AS t WHERE EXISTS (SELECT 1 FROM s WHERE keys)</c> removes every matched
/// row at once; a soft delete flags them with the shared soft-delete UPDATE over the same set. A batch with more keys than one
/// statement may bind is split only at the parameter limit, all in one command inside a savepoint
/// (<see cref="SqliteDataCommandTranslatorBase.ValueSets"/>, <see cref="SqliteDataCommandTranslatorBase.InSavepoint"/>).
/// </summary>
/// <remarks>The rows affected are the target rows deleted (or flagged).</remarks>
[TypeOption(typeof(SqliteDataCommandTranslators), "BulkDelete", RestrictToCurrentCompilation = true)]
public sealed class SqliteBulkDeleteTranslator : SqliteDataCommandTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="SqliteBulkDeleteTranslator"/> class.</summary>
    public SqliteBulkDeleteTranslator()
        : base("BulkDelete")
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
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "BulkDelete", ex.Message);
            return Task.FromResult(GenericResult<SqliteCommand>.Failure(
                SqliteDataResultCodes.ByName("DeleteTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<SqliteCommand> TranslateCommand(IDataCommand command, IStorageContainer? container)
    {
        if (container is null)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull"));
        if (container.Path is not IDatabasePath path)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath"));
        if (command is not IBulkDeleteCommand bulk)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidCommandType"));

        var data = GetCommandData(command);
        if (data is null)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("MissingInputData"), ResultDetails.Create("CommandType", command.CommandType));
        if (data is not IEnumerable rows)
            return GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidDataType"), ResultDetails.Create("ActualType", data.GetType().Name));

        var problem = SetBasedColumns.Problem(bulk.Options, container);
        if (problem is not null)
            return GenericResult<SqliteCommand>.Failure(problem);

        var sample = SetBasedUnmatchedKeys.SampleOf(command);
        return sample.IsFailure
            ? sample.ToNewResult<SqliteCommand>()
            : GenericResult<SqliteCommand>.Success(Statements(bulk.Options, rows, container, path, (command as ISoftDeletableCommand)?.SoftDelete, sample.Value));
    }

    private static SqliteCommand Statements(BulkDeleteOptions options, IEnumerable rows, IStorageContainer container, IDatabasePath path, SoftDeleteOptions? soft, int? sample)
    {
        var dialect = path.Dialect;
        var keys = SetBasedColumns.Declared(container, options.KeyColumns);
        var batch = SetBasedRows.LastPerKey(rows, keys, out _);
        var target = BuildQualifiedTableName(path);
        var keyMatch = string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"));
        var matched = $"EXISTS (SELECT 1 FROM s WHERE {keyMatch})";

        var command = CreateCommand(string.Empty);
        if (sample is { } size)
        {
            command.CommandText = ReportingUnmatched("fdw_bulk_delete", command, batch, keys, keys, size, target, container, dialect, stage =>
            {
                var staged = $"EXISTS (SELECT 1 FROM {stage} AS s WHERE {keyMatch})";
                return soft is null
                    ? $"DELETE FROM {target} AS t WHERE {staged};"
                    : $"{BuildSoftDeleteUpdate($"{target} AS t", dialect, soft, container, "CURRENT_TIMESTAMP", staged, (name, value) => AddOnce(command, name, value))};";
            });
            return command;
        }

        var sets = ValueSets(command, batch, keys, container, dialect);
        command.CommandText = InSavepoint("fdw_bulk_delete", sets.Select(values => soft is null
            ? $"{values}DELETE FROM {target} AS t WHERE {matched};"
            : $"{values}{BuildSoftDeleteUpdate($"{target} AS t", dialect, soft, container, "CURRENT_TIMESTAMP", matched, (name, value) => AddOnce(command, name, value))};"));
        return command;
    }

    // The soft delete's own parameters are the same for every group of keys, so each is added once.
    private static void AddOnce(SqliteCommand command, string name, object? value)
    {
        if (!command.Parameters.Contains($"@{name}"))
            AddParameter(command, name, value);
    }
}
