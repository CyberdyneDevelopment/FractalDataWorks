using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataStores.SqlServer.Logging;

/// <summary>
/// MessageLogging for the SQL Server catalog reader (FDW-5876): the reads it makes through a connection, the way it refuses, and the import built from them.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("SQLSERVER")]
public static partial class SqlServerCatalogLog
{
    /// <summary>Traces a read of one catalog container.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schema">The catalog schema (INFORMATION_SCHEMA or sys).</param>
    /// <param name="container">The catalog container name.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Trace,
        Message = "[SqlServerCatalogReader] Reading the catalog container '{schema}.{container}'")]
    public static partial IGenericMessage ReadStarted(ILogger logger, string schema, string container);

    /// <summary>The sys catalog has no table or view of that name in that schema.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schema">The schema name.</param>
    /// <param name="objectName">The table or view name.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 31010,
        Level = LogLevel.Error,
        Message = "[SqlServerCatalogReader] The sys catalog has no table or view '{schema}.{objectName}', so its identity and computed columns cannot be read")]
    public static partial IGenericMessage ObjectNotInCatalog(ILogger logger, string schema, string objectName);

    /// <summary>The catalog names no database.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 31011,
        Level = LogLevel.Error,
        Message = "[SqlServerCatalogReader] INFORMATION_SCHEMA.SCHEMATA names no database for the dbo schema, so the data store has no name")]
    public static partial IGenericMessage DatabaseNameNotInCatalog(ILogger logger);

    /// <summary>Traces the start of a catalog import.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="database">The database name.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "[SqlServerCatalogImporter] Importing the schema of database '{database}' through its connection")]
    public static partial IGenericMessage ImportStarted(ILogger logger, string database);

    /// <summary>Reports the end of a catalog import.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="database">The database name.</param>
    /// <param name="objectCount">How many tables, views and procedures were imported.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "[SqlServerCatalogImporter] Imported {objectCount} objects of database '{database}'")]
    public static partial IGenericMessage ImportCompleted(ILogger logger, string database, int objectCount);
}
