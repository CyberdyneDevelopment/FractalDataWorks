using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates UpdateCommand to PostgreSQL UPDATE statement.
/// </summary>
/// <remarks>
/// <para>
/// Builds PostgreSQL UPDATE statements with:
/// <list type="bullet">
/// <item>UPDATE - container's physical name with double-quoted identifiers</item>
/// <item>SET clause - from container schema and command data (excludes PK and identity)</item>
/// <item>WHERE clause - from Filter expression or primary key match</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "Update", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlUpdateTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlUpdateTranslator"/> class.
    /// </summary>
    public PostgreSqlUpdateTranslator()
        : base("Update")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to typed overload or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is IFilterableCommand filterableCommand)
        {
            return Translate(filterableCommand, container, cancellationToken);
        }

        return Task.FromResult(
            GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlTranslatorLog.InvalidCommandType(
                    NullLogger<PostgreSqlUpdateTranslator>.Instance,
                    "PostgreSqlUpdateTranslator",
                    "IFilterableCommand with IDataCommandWithInput",
                    command.GetType().Name)));
    }

    /// <summary>
    /// Translates an IFilterableCommand with input data to a PostgreSQL UPDATE statement.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static - overload resolution pattern requires instance method")]
    public Task<IGenericResult<NpgsqlCommand>> Translate(
        IFilterableCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(
                        PostgreSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "UpdateCommand")));
            }

            var npgsqlCommand = BuildUpdateStatement(container, dbPath, dataObj, command.Filter);

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (NullPrimaryKeyException ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("NullPrimaryKeyValue"),
                    ResultDetails.Create("PrimaryKeyField", ex.PrimaryKeyField)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("UpdateTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a complete PostgreSQL UPDATE statement.
    /// </summary>
#pragma warning disable MA0051 // Method is too long
    [ConventionOverride(MaxCyclomaticComplexity = 15)]
    private static NpgsqlCommand BuildUpdateStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        object data,
        IFilterExpression? filter)
#pragma warning restore MA0051
    {
        var dialect = dbPath.Dialect;

        var pkFieldName = container.GetPrimaryKeyFieldName();
        // Get columns from schema (exclude PK, identity, and computed columns)
        var updateFields = container.Schema.Fields
            .Where(f => !string.Equals(f.Name, pkFieldName, StringComparison.OrdinalIgnoreCase) && !f.IsIdentity && !f.IsComputed)
            .ToList();

        if (updateFields.Count == 0)
        {
            throw new InvalidOperationException($"Container {container.Name} has no updatable fields");
        }

        // Only include fields that have matching properties on the data object
        var dataType = data.GetType();
        var fieldNames = updateFields.Select(f => f.Name)
            .Where(f => dataType.GetProperty(f) != null)
            .ToList();

        if (fieldNames.Count == 0)
        {
            throw new InvalidOperationException(
                $"Data object has no properties matching updatable fields for container {container.Name}");
        }

        // Build SET clause with dialect-quoted identifiers
        var p = dialect.ParameterPrefix;
        var setClause = string.Join(", ", fieldNames.Select(f => $"{dialect.QuoteIdentifier(f)} = {p}set_{f}"));

        // Build initial UPDATE statement
        var sql = $"UPDATE {BuildQualifiedTableName(dbPath)} SET {setClause}";

        var command = CreateCommand(sql);

        // Add SET parameters from data object
        foreach (var fieldName in fieldNames)
        {
            var property = dataType.GetProperty(fieldName);
            if (property != null)
            {
                AddParameter(command, $"set_{fieldName}", property.GetValue(data));
            }
        }

        // Build WHERE clause
        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v), parameterPrefix: "@where_");
            command.CommandText += $" WHERE {whereClause}";
        }
        else
        {
            var pkName = container.GetPrimaryKeyFieldName();
            var pkField = pkName != null ? container.Schema?.Fields?.FirstOrDefault(f => string.Equals(f.Name, pkName, StringComparison.OrdinalIgnoreCase)) : null;
            if (pkField == null)
            {
                throw new InvalidOperationException($"Container {container.Name} has no primary key and no filter provided");
            }

            var pkProperty = dataType.GetProperty(pkField.Name);
            if (pkProperty != null)
            {
                var pkValue = pkProperty.GetValue(data);
                if (pkValue == null)
                {
                    throw new NullPrimaryKeyException(pkField.Name);
                }

                AddParameter(command, "where_pk", pkValue);
                command.CommandText += $" WHERE {dialect.QuoteIdentifier(pkField.Name)} = {p}where_pk";
            }
        }

        return command;
    }

    /// <summary>
    /// Sentinel exception used to signal a null primary key value to the Translate method.
    /// </summary>
    private sealed class NullPrimaryKeyException : InvalidOperationException
    {
        public NullPrimaryKeyException()
        {
            PrimaryKeyField = string.Empty;
        }

        public NullPrimaryKeyException(string fieldName)
            : base($"Primary key '{fieldName}' is null")
        {
            PrimaryKeyField = fieldName;
        }

        public NullPrimaryKeyException(string message, Exception innerException)
            : base(message, innerException)
        {
            PrimaryKeyField = string.Empty;
        }

        public string PrimaryKeyField { get; }
    }
}
