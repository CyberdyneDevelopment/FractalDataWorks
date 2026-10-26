using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Sql;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates UpdateCommand to T-SQL UPDATE statement.
/// </summary>
/// <remarks>
/// <para>
/// Builds T-SQL UPDATE statements with:
/// <list type="bullet">
/// <item>UPDATE - container's physical name</item>
/// <item>SET clause - from container schema and command data (excludes PK and identity)</item>
/// <item>WHERE clause - from Filter expression or primary key match</item>
/// </list>
/// </para>
/// <para>
/// Excludes primary key and identity columns from SET clause.
/// Uses Filter expression for WHERE clause, or falls back to primary key if available.
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Update", RestrictToCurrentCompilation = true)]
public sealed class MsSqlUpdateTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlUpdateTranslator"/> class.
    /// </summary>
    public MsSqlUpdateTranslator()
        : base("Update")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to typed overload or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is IFilterableCommand filterableCommand)
        {
            return Translate(filterableCommand, container, cancellationToken);
        }

        return Task.FromResult(
            GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(
                    NullLogger<MsSqlUpdateTranslator>.Instance,
                    "MsSqlUpdateTranslator",
                    "IFilterableCommand with IDataCommandWithInput",
                    command.GetType().Name)));
    }

    /// <summary>
    /// Translates an IFilterableCommand with input data to a T-SQL UPDATE statement.
    /// Overload resolution ensures this method is called for UpdateCommand instances.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static - overload resolution pattern requires instance method")]
    public Task<IGenericResult<SqlCommand>> Translate(
        IFilterableCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            // Validate container path is a database path
            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            // Get entity data from command
            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "UpdateCommand")));
            }

            // A soft-deleting update also flags the matching rows deleted (FDW-1107).
            var soft = MsSqlSoftDeleteSql.For(command, container, dbPath.Dialect, allowAbsentSet: false);
            if (soft.Problem is not null)
                return Task.FromResult(MsSqlSoftDeleteSql.Refusal(soft));

            // Build UPDATE statement with strongly-typed Filter property (no reflection!)
            var sqlCommand = BuildUpdateStatement(container, dbPath, dataObj, command.Filter, soft, ReturnedColumnsOf(command, container));

            return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
        }
        catch (NullPrimaryKeyException ex)
        {
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("NullPrimaryKeyValue"),
                    ResultDetails.Create("PrimaryKeyField", ex.PrimaryKeyField)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("UpdateTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// The DDL convention's Modify* audit columns and the expressions their column defaults use.
    /// </summary>
    /// <remarks>
    /// Why: the audit block is DB-filled and never app-set (system-provided), so an INSERT leaves it to
    /// the column defaults. An in-place UPDATE fires no default, so without a restamp ModifyDate/ModifyBy
    /// keep their insert-time values forever. Restamping with the same expressions an INSERT would use
    /// keeps the columns honest. The configuration translators already stamp ModifyDate this way on their
    /// own UPDATEs. The set is the convention's closed list, matched by name.
    /// </remarks>
    private static readonly (string Column, string Expression)[] ModifyAuditColumns =
    [
        ("ModifyDate", "SYSDATETIMEOFFSET()"),
        ("ModifyBy", "SUSER_SNAME()"),
        ("ModifyOnBehalfOf", "SUSER_SNAME()"),
    ];

    /// <summary>
    /// Builds a complete T-SQL UPDATE statement.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // SQL UPDATE builder with field filtering, reflection, and conditional WHERE clause (filter vs primary key)
    private static SqlCommand BuildUpdateStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        object data,
        IFilterExpression? filter,
        MsSqlSoftDeleteFragments soft,
        IReadOnlyList<string> returned)
    {
        var dialect = dbPath.Dialect;

        var pkFieldName = container.GetPrimaryKeyFieldName();
        // Get columns from schema (exclude PK, identity, computed, and system-provided
        // columns). System-provided columns (e.g. RowId via newsequentialid(), audit
        // CreateDate/ModifyDate, IsCurrent/IsDeleted version flags) must not appear in
        // SET — including RowId there violates child FK constraints (FK_*_RowId) on
        // version-on-write parents.
        var updateFields = container.Schema.Fields
            .Where(f =>
                !string.Equals(f.Name, pkFieldName, StringComparison.OrdinalIgnoreCase)
                && !f.IsIdentity
                && !f.IsComputed
                && !f.IsSystemProvided)
            .ToList();

        if (updateFields.Count == 0)
        {
            throw new InvalidOperationException($"Container {container.Name} has no updatable fields");
        }

        // Only include fields that have matching properties on the data object
        var dataType = data.GetType();
        var fieldNames = updateFields.Select(f => f.Name)
            .Where(f => dataType.GetProperty(f) != null)
            .Where(f => !ModifyAuditColumns.Any(a => string.Equals(a.Column, f, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (fieldNames.Count == 0)
        {
            throw new InvalidOperationException(
                $"Data object has no properties matching updatable fields for container {container.Name}");
        }

        // Build SET clause
        var prefix = dialect.ParameterPrefix;
        var auditStamps = ModifyAuditColumns
            .Where(a => container.Schema.Fields.Any(f => string.Equals(f.Name, a.Column, StringComparison.OrdinalIgnoreCase)))
            .Select(a => $"{dialect.QuoteIdentifier(a.Column)} = {a.Expression}");
        var setClause = string.Join(", ", fieldNames.Select(f => $"{dialect.QuoteIdentifier(f)} = {prefix}set_{f}").Concat(auditStamps).Concat(soft.SetClauses(null, onlyWhenMarked: true)));

        // Build initial UPDATE statement
        var sql = $"UPDATE {BuildQualifiedTableName(dbPath)} SET {setClause}"
            + (returned.Count == 0 ? string.Empty : $" OUTPUT {string.Join(", ", returned.Select(c => $"inserted.{dialect.QuoteIdentifier(c)}"))}");

        // Get SqlCommand
        var command = CreateCommand(sql);
        command.Parameters.AddRange(soft.Parameters.ToArray());

        // Add SET parameters from data object
        foreach (var fieldName in fieldNames)
        {
            var property = dataType.GetProperty(fieldName);
            if (property != null)
                AddParameter(command, $"set_{fieldName}", property.GetValue(data), property.PropertyType);
        }

        // Build WHERE clause
        if (filter?.Root != null)
        {
            // Use provided filter
            var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v), null, $"{prefix}where_");
            command.CommandText += $" WHERE {whereClause}";
        }
        else
        {
            // Fall back to primary key if available
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
                    // Return null sentinel so Translate can produce a specific result code
                    throw new NullPrimaryKeyException(pkField.Name);
                }

                AddParameter(command, "where_pk", pkValue);
                command.CommandText += $" WHERE {dialect.QuoteIdentifier(pkField.Name)} = {prefix}where_pk";
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
