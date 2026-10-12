using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Search.Endpoints;

/// <summary>
/// MessageLogging for the Find endpoint.
/// EventId range: 4520-4529
/// </summary>
[MessageLoggingTypeCode("SEARCHENDPOINTS")]
public static partial class FindEndpointLog
{
    /// <summary>Logs when a find operation starts.</summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Find started: searchTerm='{searchTerm}' container='{containerName}' fieldCount={fieldCount}")]
    public static partial IGenericMessage FindStarted(
        ILogger logger,
        string searchTerm,
        string containerName,
        int fieldCount);

    /// <summary>Logs when a find operation completes successfully.</summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Find completed: {resultCount} results in {elapsedMs:F1}ms")]
    public static partial IGenericMessage FindCompleted(
        ILogger logger,
        int resultCount,
        double elapsedMs);

    /// <summary>Logs when a find operation returns no results.</summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "Find returned no results for searchTerm='{searchTerm}' in container='{containerName}'")]
    public static partial IGenericMessage FindNoResults(
        ILogger logger,
        string searchTerm,
        string containerName);

    /// <summary>Logs that a find was answered with its failure instead of matches.</summary>
    // Why Information: the gateway already logged why the find failed, at that failure's own level. This line only
    // records that the endpoint answered it once, with that status.
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "Find in container '{containerName}' was answered with its failure; no matches were returned")]
    public static partial IGenericMessage FindNotAnswered(
        ILogger logger,
        string containerName);

    /// <summary>Logs trace when find request is received.</summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Trace,
        Message = "Find request received: dataStore='{dataStoreName}' path='{pathName}' container='{containerName}' maxResults={maxResults}")]
    public static partial IGenericMessage FindRequestReceived(
        ILogger logger,
        string dataStoreName,
        string pathName,
        string containerName,
        int maxResults);

    /// <summary>Logs trace when find command is dispatched to DataGateway.</summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Trace,
        Message = "Dispatching find command to DataGateway for container '{containerName}'")]
    public static partial IGenericMessage DispatchingFindCommand(
        ILogger logger,
        string containerName);
}
