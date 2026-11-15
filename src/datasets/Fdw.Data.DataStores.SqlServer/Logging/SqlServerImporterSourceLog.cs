using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataStores.SqlServer.Logging;

/// <summary>
/// MessageLogging for the ways <see cref="SqlServerSchemaImporter"/> refuses its <c>source</c> (FDW-5951): the source is the name of a configured
/// connection, resolved with the provider the options carry, and each refusal is its own message. None of them carries the source's value: a value
/// that is a connection string may hold a password.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("SQLSERVER")]
public static partial class SqlServerImporterSourceLog
{
    /// <summary>The source is empty.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 21020,
        Level = LogLevel.Error,
        Message = "[SqlServerSchemaImporter] The source names no connection: it must be the name of a configured connection")]
    public static partial IGenericMessage SourceNamesNoConnection(ILogger logger);

    /// <summary>The source is a connection string, which the importer no longer accepts.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 21021,
        Level = LogLevel.Error,
        Message = "[SqlServerSchemaImporter] The source is a connection string: it must be the name of a configured connection, and a connection string is no longer accepted")]
    public static partial IGenericMessage SourceIsAConnectionString(ILogger logger);

    /// <summary>The connection name cannot be resolved because the options carry no provider.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connection">The connection name.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 61020,
        Level = LogLevel.Error,
        Message = "[SqlServerSchemaImporter] The options carry no connection provider (IDataConnectionProvider), so the connection '{connection}' cannot be resolved")]
    public static partial IGenericMessage NoConnectionProvider(ILogger logger, string connection);

    /// <summary>The provider answered without a connection for the name.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connection">The connection name.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 31020,
        Level = LogLevel.Error,
        Message = "[SqlServerSchemaImporter] The connection '{connection}' resolves to no connection")]
    public static partial IGenericMessage ConnectionResolvesToNone(ILogger logger, string connection);

    /// <summary>Traces that the source's connection was resolved.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connection">The connection name.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Trace,
        Message = "[SqlServerSchemaImporter] Resolved the connection '{connection}' through the provider in the options")]
    public static partial IGenericMessage ConnectionResolved(ILogger logger, string connection);
}
