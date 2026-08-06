using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

            var dataObj = GetCommandData(command);
            if (dataObj == null)
                return Task.FromResult(
                    GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "UpdateCommand")));

            return Task.FromResult(BuildUpdateStatement(container, dbPath, dataObj, command.Filter));
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

#pragma warning disable MA0051
    [ConventionOverride(MaxCyclomaticComplexity = 15)]
    private static IGenericResult<SqliteCommand> BuildUpdateStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        object data,
        IFilterExpression? filter)
#pragma warning restore MA0051
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

        var dataType = data.GetType();
        var fieldNames = updateFields.Select(f => f.Name)
            .Where(f => dataType.GetProperty(f) != null)
            .ToList();

        if (fieldNames.Count == 0)
            throw new InvalidOperationException($"Data object has no properties matching updatable fields for container {container.Name}");

        var prefix = dialect.ParameterPrefix;
        var setClause = string.Join(", ", fieldNames.Select(f => $"{dialect.QuoteIdentifier(f)} = {prefix}set_{f}"));
        var sql = $"UPDATE {BuildQualifiedTableName(dbPath)} SET {setClause}";

        var command = CreateCommand(sql);

        foreach (var fieldName in fieldNames)
        {
            var property = dataType.GetProperty(fieldName);
            if (property != null)
                AddParameter(command, $"set_{fieldName}", property.GetValue(data));
        }

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

            var pkProperty = dataType.GetProperty(pkField.Name);
            if (pkProperty != null)
            {
                var pkValue = pkProperty.GetValue(data);
                // Why: a null PK value is an EXPECTED failure (bad input data), not an
                // exceptional condition — fail loud with a structured ResultCode instead of
                // throwing+catching, matching the MsSql/PostgreSql sibling translators.
                if (pkValue == null)
                    return GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("NullPrimaryKeyValue"),
                        ResultDetails.Create("PrimaryKeyField", pkField.Name));

                AddParameter(command, "where_pk", pkValue);
                command.CommandText += $" WHERE {dialect.QuoteIdentifier(pkField.Name)} = {prefix}where_pk";
            }
        }

        return GenericResult<SqliteCommand>.Success(command);
    }
}
