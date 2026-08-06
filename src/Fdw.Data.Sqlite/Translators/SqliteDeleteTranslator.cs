using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.Sqlite;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates DeleteCommand to a SQLite DELETE statement.
/// Always requires a WHERE clause to prevent accidental full-table deletion.
/// </summary>
[TypeOption(typeof(SqliteDataCommandTranslators), "Delete", RestrictToCurrentCompilation = true)]
public sealed class SqliteDeleteTranslator : SqliteDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDeleteTranslator"/> class.
    /// </summary>
    public SqliteDeleteTranslator()
        : base("Delete")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqliteCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is IFilterableCommand filterableCommand)
            return Translate(filterableCommand, container, cancellationToken);

        return Task.FromResult(
            GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidCommandType")));
    }

    /// <summary>
    /// Translates an <see cref="IFilterableCommand"/> to a SQLite DELETE statement.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static — overload resolution requires instance method")]
    public Task<IGenericResult<SqliteCommand>> Translate(
        IFilterableCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull")));

            if (container.Path is not IDatabasePath dbPath)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath")));

            var filter = command.Filter;
            if (filter == null || filter.Root == null)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("MissingDeleteFilter")));

            return Task.FromResult(GenericResult<SqliteCommand>.Success(BuildDeleteStatement(dbPath, filter)));
        }
        catch (Exception ex)
        {
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "Delete", ex.Message);
            return Task.FromResult(
                GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("DeleteTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static SqliteCommand BuildDeleteStatement(IDatabasePath dbPath, IFilterExpression filter)
    {
        var dialect = dbPath.Dialect;
        var sql = $"DELETE FROM {BuildQualifiedTableName(dbPath)}";
        var command = CreateCommand(sql);
        var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v));
        command.CommandText += $" WHERE {whereClause}";
        return command;
    }
}
