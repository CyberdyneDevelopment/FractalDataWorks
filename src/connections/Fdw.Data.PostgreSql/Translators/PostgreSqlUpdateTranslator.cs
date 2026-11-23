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
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Fdw.Services.Connections.Sql;

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

            RefuseSoftDeleteOnUpdate(command, container);

            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(
                        PostgreSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "UpdateCommand")));
            }

            var npgsqlCommand = BuildUpdateStatement(container, dbPath, dataObj, command.Filter, ReturnedColumnsOf(command, container));

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (MissingPrimaryKeyException ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("MissingPrimaryKeyValue"),
                    ResultDetails.Create("Container", ex.ContainerName, "PrimaryKeyField", ex.PrimaryKeyField)));
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
    [ConventionOverride(MaxCyclomaticComplexity = 15)]
    private static NpgsqlCommand BuildUpdateStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        object data,
        IFilterExpression? filter,
        IReadOnlyList<string> returned)
    {
        var dialect = dbPath.Dialect;

        // The primary key's columns are never in SET, with a filter or without one (FDW-6309): without a filter the key is the WHERE,
        // and with one a record that carries its key would write that one key value onto every row the filter matches.
        var keyColumns = container.PrimaryKeyFieldNames();
        // Get columns from schema (exclude the key, identity, and computed columns)
        var updateFields = container.Schema.Fields
            .Where(f => !keyColumns.Contains(f.Name, StringComparer.OrdinalIgnoreCase) && !f.IsIdentity && !f.IsComputed)
            .ToList();

        if (updateFields.Count == 0)
        {
            throw new InvalidOperationException($"Container {container.Name} has no updatable fields");
        }

        var settable = SettableFields(updateFields, data);
        var fieldNames = settable.Select(s => s.Name).ToList();

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
        foreach (var (name, value) in settable)
        {
            AddParameter(command, $"set_{name}", value);
        }

        // Build WHERE clause
        if (filter?.Root != null)
        {
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v), parameterPrefix: "@where_");
            command.CommandText += $" WHERE {whereClause}";
        }
        else
        {
            // The statement states no rows, so the container's primary key does: every column of it, never the first alone, or an UPDATE
            // writes every row that shares one column's value (FDW-6309). The key columns are the WHERE and so are not in SET.
            var keyNames = container.PrimaryKeyFieldNames();
            if (keyNames.Count == 0)
            {
                throw new InvalidOperationException($"Container {container.Name} has no primary key and no filter provided");
            }

            var conditions = new List<string>(keyNames.Count);
            for (var i = 0; i < keyNames.Count; i++)
            {
                var keyName = keyNames[i];
                var keyField = container.Schema.Fields.FirstOrDefault(f => string.Equals(f.Name, keyName, StringComparison.OrdinalIgnoreCase));
                if (keyField == null)
                {
                    throw new InvalidOperationException($"Container {container.Name} has no primary key and no filter provided");
                }

                // No filter and no key in the record names no rows; an UPDATE without a WHERE writes every row, so it is refused.
                if (!RowValues.TryRead(data, keyField.Name, out var keyValue, out _))
                {
                    throw new MissingPrimaryKeyException(container.Name, keyField.Name);
                }

                if (keyValue == null)
                {
                    throw new NullPrimaryKeyException(keyField.Name);
                }

                var parameterName = keyNames.Count == 1 ? "where_pk" : $"where_pk{i}";
                AddParameter(command, parameterName, keyValue);
                conditions.Add($"{dialect.QuoteIdentifier(keyField.Name)} = {p}{parameterName}");
            }

            command.CommandText += $" WHERE {string.Join(" AND ", conditions)}";
        }

        if (returned.Count > 0)
            command.CommandText += $" RETURNING {string.Join(", ", returned.Select(c => dialect.QuoteIdentifier(c)))}";

        return command;
    }

    // Only the fields the record has, in the schema's order. The record is read the one way every route reads it (RowValues), so a dictionary record, which is what a
    // pipeline's Update load carries, sets its columns as an entity record does.
    private static List<(string Name, object? Value)> SettableFields(IEnumerable<IField> updateFields, object data)
    {
        var settable = new List<(string Name, object? Value)>();
        foreach (var field in updateFields)
        {
            if (RowValues.TryRead(data, field.Name, out var value, out _))
            {
                settable.Add((field.Name, value));
            }
        }

        return settable;
    }

    /// <summary>
    /// Sentinel exception used to signal a record that does not carry the primary key to the Translate method.
    /// </summary>
    private sealed class MissingPrimaryKeyException : InvalidOperationException
    {
        public MissingPrimaryKeyException()
        {
            ContainerName = string.Empty;
            PrimaryKeyField = string.Empty;
        }

        public MissingPrimaryKeyException(string message)
            : base(message)
        {
            ContainerName = string.Empty;
            PrimaryKeyField = string.Empty;
        }

        public MissingPrimaryKeyException(string message, Exception innerException)
            : base(message, innerException)
        {
            ContainerName = string.Empty;
            PrimaryKeyField = string.Empty;
        }

        public MissingPrimaryKeyException(string containerName, string fieldName)
            : base($"Record for container '{containerName}' does not carry primary key '{fieldName}'")
        {
            ContainerName = containerName;
            PrimaryKeyField = fieldName;
        }

        public string ContainerName { get; }

        public string PrimaryKeyField { get; }
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
