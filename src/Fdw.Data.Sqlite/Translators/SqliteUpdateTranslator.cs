using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Sql;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates UpdateCommand to a SQLite UPDATE statement.
/// </summary>
[TypeOption(typeof(SqliteDataCommandTranslators), "Update", RestrictToCurrentCompilation = true)]
public sealed class SqliteUpdateTranslator : SqliteDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteUpdateTranslator"/> class.
    /// </summary>
    public SqliteUpdateTranslator()
        : base("Update")
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
    /// Translates an <see cref="IFilterableCommand"/> with input data to a SQLite UPDATE statement.
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

            RefuseSoftDeleteOnUpdate(command, container);

            var dataObj = GetCommandData(command);
            if (dataObj == null)
                return Task.FromResult(
                    GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "UpdateCommand")));

            return Task.FromResult(BuildUpdateStatement(container, dbPath, dataObj, command.Filter, ReturnedColumnsOf(command, container)));
        }
        catch (Exception ex)
        {
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "Update", ex.Message);
            return Task.FromResult(
                GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("UpdateTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    [ConventionOverride(MaxCyclomaticComplexity = 15)]
    private static IGenericResult<SqliteCommand> BuildUpdateStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        object data,
        IFilterExpression? filter,
        IReadOnlyList<string> returned)
    {
        var dialect = dbPath.Dialect;
        var pkFieldName = container.GetPrimaryKeyFieldName();

        var updateFields = container.Schema.Fields
            .Where(f =>
                !string.Equals(f.Name, pkFieldName, StringComparison.OrdinalIgnoreCase)
                && !f.IsIdentity
                && !f.IsComputed
                && !f.IsSystemProvided)
            .ToList();

        if (updateFields.Count == 0)
            throw new InvalidOperationException($"Container {container.Name} has no updatable fields");

        var settable = SettableFields(updateFields, data);
        var fieldNames = settable.Select(s => s.Name).ToList();

        if (fieldNames.Count == 0)
            throw new InvalidOperationException($"Data object has no properties matching updatable fields for container {container.Name}");

        var prefix = dialect.ParameterPrefix;
        var setClause = string.Join(", ", fieldNames.Select(f => $"{dialect.QuoteIdentifier(f)} = {prefix}set_{f}"));
        var sql = $"UPDATE {BuildQualifiedTableName(dbPath)} SET {setClause}";

        var command = CreateCommand(sql);

        foreach (var (name, value) in settable)
            AddParameter(command, $"set_{name}", value);

        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v), null, $"{prefix}where_");
            command.CommandText += $" WHERE {whereClause}";
        }
        else
        {
            var pkName = container.GetPrimaryKeyFieldName();
            var pkField = pkName != null ? container.Schema?.Fields?.FirstOrDefault(f => string.Equals(f.Name, pkName, StringComparison.OrdinalIgnoreCase)) : null;
            if (pkField == null)
                throw new InvalidOperationException($"Container {container.Name} has no primary key and no filter provided");

            // No filter and no key in the record names no rows; an UPDATE without a WHERE writes every row, so it is refused.
            if (!RowValues.TryRead(data, pkField.Name, out var pkValue, out _))
                return GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("MissingPrimaryKeyValue"),
                    ResultDetails.Create("Container", container.Name, "PrimaryKeyField", pkField.Name));

            if (pkValue == null)
                return GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("NullPrimaryKeyValue"),
                    ResultDetails.Create("PrimaryKeyField", pkField.Name));

            AddParameter(command, "where_pk", pkValue);
            command.CommandText += $" WHERE {dialect.QuoteIdentifier(pkField.Name)} = {prefix}where_pk";
        }

        if (returned.Count > 0)
            command.CommandText += $" RETURNING {string.Join(", ", returned.Select(c => dialect.QuoteIdentifier(c)))}";

        return GenericResult<SqliteCommand>.Success(command);
    }

    // Only the fields the record has, in the schema's order. The record is read the one way every route reads it (RowValues), so a dictionary record, which is what a
    // pipeline's Update load carries, sets its columns as an entity record does.
    private static List<(string Name, object? Value)> SettableFields(IEnumerable<IField> updateFields, object data)
    {
        var settable = new List<(string Name, object? Value)>();
        foreach (var field in updateFields)
        {
            if (RowValues.TryRead(data, field.Name, out var value, out _))
                settable.Add((field.Name, value));
        }

        return settable;
    }
}
