using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for MsSql data command translators.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlTranslatorLog
{
    /// <summary>
    /// Logs when a translator receives an invalid command type.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Translator '{translatorName}' expected {expectedType} but received {actualType}")]
    public static partial IGenericMessage InvalidCommandType(
        ILogger logger,
        string translatorName,
        string expectedType,
        string actualType);

    /// <summary>
    /// Logs when a container has no declared fields and no projection — the translator
    /// refuses to emit <c>SELECT *</c>.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Cannot translate query for container '{containerName}': no declared fields and no projection. SELECT * is not permitted.")]
    public static partial IGenericMessage NoFieldsToProject(
        ILogger logger,
        string containerName);

    /// <summary>
    /// Logs when the container passed to an MsSql translator does not implement IDataContainer.
    /// MsSql translators require structured key/field metadata only available on IDataContainer.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSql translator cannot operate on container '{containerName}': container does not implement IDataContainer. Only structured data containers (MsSqlDataContainer) are valid for MsSql translators.")]
    public static partial IGenericMessage ContainerNotDataContainer(
        ILogger logger,
        string containerName);

    /// <summary>Logs the SELECT shape a query is translated to.</summary>
    [MessageLogging(
        EventId = 12075,
        Level = LogLevel.Trace,
        Message = "Translating query on container '{containerName}' as {shape} ({joinCount} join(s))")]
    public static partial IGenericMessage TranslatingQuery(
        ILogger logger,
        string containerName,
        string shape,
        int joinCount);

    /// <summary>Logged when a query asks for aggregation and JOINs together, which this translator does not build.</summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "Query on container '{containerName}' combines aggregation with {joinCount} join(s); the MsSql query translator builds an aggregate over one container only")]
    public static partial IGenericMessage AggregationWithJoins(
        ILogger logger,
        string containerName,
        int joinCount);

    /// <summary>Logged when a query is translated with no container to read.</summary>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Error,
        Message = "Query translation was handed no container, so there is no table to read")]
    public static partial IGenericMessage QueryContainerNull(ILogger logger);

    /// <summary>Logged when a query's container is not on a database path.</summary>
    [MessageLogging(
        EventId = 21004,
        Level = LogLevel.Error,
        Message = "Query on container '{containerName}' cannot be translated to SQL: its path is a {pathType}, not a database path")]
    public static partial IGenericMessage QueryPathNotDatabase(
        ILogger logger,
        string containerName,
        string pathType);

    /// <summary>Logged when building the SQL for a query throws.</summary>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Critical,
        Message = "Translating the query on container '{containerName}' threw: {reason}")]
    public static partial IGenericMessage QueryTranslationThrew(
        ILogger logger,
        System.Exception exception,
        string containerName,
        string reason);
}
