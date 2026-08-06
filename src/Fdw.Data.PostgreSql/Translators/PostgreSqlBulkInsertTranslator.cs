using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates BulkInsertCommand to PostgreSQL COPY FROM STDIN operation for maximum performance.
/// </summary>
/// <remarks>
/// <para>
/// Uses PostgreSQL COPY FROM STDIN BINARY for high-performance bulk insert operations.
/// This is the PostgreSQL equivalent of SQL Server's SqlBulkCopy.
/// </para>
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "BulkInsert", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlBulkInsertTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlBulkInsertTranslator"/> class.
    /// </summary>
    public PostgreSqlBulkInsertTranslator()
        : base("BulkInsert")
    {
    }

    /// <summary>
    /// Translates a BulkInsertCommand to a PostgreSQL COPY FROM STDIN operation.
    /// </summary>
    /// <remarks>
    /// NOTE: This translator returns a "wrapper" NpgsqlCommand that encapsulates
    /// COPY metadata. The actual COPY operation is executed by PostgreSqlConnection
    /// when it detects this special command type via the StoredProcedure marker.
    /// </remarks>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
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
                        ResultDetails.Create("CommandType", "BulkInsertCommand")));
            }

            if (dataObj is not IEnumerable collection)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(
                        PostgreSqlDataResultCodes.ByName("InvalidDataType"),
                        ResultDetails.Create("TranslatorName", "PostgreSqlBulkInsertTranslator", "ActualType", dataObj.GetType().Name)));
            }

            var npgsqlCommand = BuildCopyCommand(container, dbPath, collection);

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("BulkInsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a special NpgsqlCommand that wraps COPY FROM STDIN metadata.
    /// PostgreSqlConnection will detect this and use NpgsqlBinaryImporter instead of ExecuteNonQuery.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private static NpgsqlCommand BuildCopyCommand(
        IStorageContainer container,
        IDatabasePath dbPath,
        IEnumerable collection)
    {
        var dialect = dbPath.Dialect;

        // Get insertable fields
        var fields = container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed)
            .ToList();

        if (fields.Count == 0)
        {
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
        }

        // Build COPY SQL: COPY "schema"."table" ("col1", "col2") FROM STDIN BINARY
        var columnList = string.Join(", ", fields.Select(f => dialect.QuoteIdentifier(f.Name)));
        var qualifiedTableName = BuildQualifiedTableName(dbPath);
        var copySql = $"COPY {qualifiedTableName} ({columnList}) FROM STDIN BINARY";

        // Store as marker command for PostgreSqlConnection to detect
        var command = CreateCommand($"-- BULK INSERT MARKER: {qualifiedTableName}");
        command.CommandType = System.Data.CommandType.StoredProcedure; // Marker for bulk operation

        // Store metadata in Parameters for PostgreSqlConnection to use
        command.Parameters.Add(new NpgsqlParameter("__BulkCopy_CopySql", copySql));
        command.Parameters.Add(new NpgsqlParameter("__BulkCopy_Destination", BuildSchemaQualifiedTableName(dbPath)));
        command.Parameters.Add(new NpgsqlParameter("__BulkCopy_ColumnMappings",
            string.Join(",", fields.Select(f => f.Name))));

        // Serialize entity data into parameter for PostgreSqlConnection
        var entities = new List<object>();
        foreach (var entity in collection)
        {
            entities.Add(entity);
        }

        command.Parameters.Add(new NpgsqlParameter("__BulkCopy_Entities", entities));

        return command;
    }
}
