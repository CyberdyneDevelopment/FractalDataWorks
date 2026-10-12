#pragma warning disable CS1591
using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Conventions;
using Fdw.Results;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Types.MsSql;

/// <summary>
/// Ensures the types schema and tables exist in the database.
/// </summary>
public static class TypesSchemaInitializer
{
    private static readonly string[] GoSeparators = ["\r\nGO\r\n", "\nGO\n", "\r\nGO", "\nGO"];

    /// <summary>
    /// Ensures the types schema and all required tables exist in the database.
    /// </summary>
    /// <param name="connectionString">Connection string to the database.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // DDL initialization — validation, resource loading, batch execution, error handling
    public static async Task<IGenericResult> EnsureTypesSchema(
        string connectionString,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return GenericResult.Failure(MsSqlTypesResultCodes.InvalidConnectionString);
        }

        try
        {
            logger?.LogInformation("Ensuring types schema exists in database");

            var ddl = GetEmbeddedDdl();
            if (string.IsNullOrWhiteSpace(ddl))
            {
                logger?.LogError("Failed to load embedded DDL resource");
                return GenericResult.Failure(MsSqlTypesResultCodes.DdlResourceNotFound);
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var batches = ddl.Split(GoSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var batch in batches)
            {
                var trimmedBatch = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmedBatch))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.CommandText = trimmedBatch;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            logger?.LogInformation("Types schema initialization complete");
            return GenericResult.Success();
        }
        catch (SqlException ex)
        {
            logger?.LogError(ex, "SQL error ensuring types schema: {Message}", ex.Message);
            return GenericResult.Failure(MsSqlTypesResultCodes.SchemaInitializationFailed);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error ensuring types schema: {Message}", ex.Message);
            return GenericResult.Failure(MsSqlTypesResultCodes.SchemaInitializationFailed);
        }
    }

    private static string GetEmbeddedDdl()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Fdw.Types.MsSql.Sql.TypesSchema.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
