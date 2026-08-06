using System;
using System.Linq;
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
/// Translates InsertCommand to a SQLite INSERT statement.
/// Returns the last inserted row ID via <c>SELECT last_insert_rowid()</c>.
/// </summary>
[TypeOption(typeof(SqliteDataCommandTranslators), "Insert", RestrictToCurrentCompilation = true)]
public sealed class SqliteInsertTranslator : SqliteDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteInsertTranslator"/> class.
    /// </summary>
    public SqliteInsertTranslator()
        : base("Insert")
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
            if (container == null)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull")));

            if (container.Path is not IDatabasePath dbPath)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath")));

            var dataObj = GetCommandData(command);
            if (dataObj == null)
                return Task.FromResult(
                    GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "InsertCommand")));

            return Task.FromResult(GenericResult<SqliteCommand>.Success(BuildInsertStatement(container, dbPath, dataObj)));
        }
        catch (Exception ex)
        {
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "Insert", ex.Message);
            return Task.FromResult(
                GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("InsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static SqliteCommand BuildInsertStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        object data)
    {
        var fields = container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed && !f.IsSystemProvided)
            .ToList();

        if (fields.Count == 0)
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");

        var allFieldNames = fields.Select(f => f.Name).ToList();
        var dataType = data.GetType();
        var fieldNames = allFieldNames.Where(f => dataType.GetProperty(f) != null).ToList();

        if (fieldNames.Count == 0)
            throw new InvalidOperationException($"Data object has no properties matching insertable fields for container {container.Name}");

        var dialect = dbPath.Dialect;
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));
        var prefix = dialect.ParameterPrefix;
        var paramList = string.Join(", ", fieldNames.Select(f => $"{prefix}{f}"));

        // Why: SQLite uses last_insert_rowid() (not SCOPE_IDENTITY / RETURNING) for the inserted row ID.
        var sql = $"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}) VALUES ({paramList}); SELECT last_insert_rowid();";

        var command = CreateCommand(sql);
        AddParametersFromObject(data, fieldNames, (n, v) => AddParameter(command, n, v));
        return command;
    }
}
