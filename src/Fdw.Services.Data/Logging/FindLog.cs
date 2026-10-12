using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for Find (cross-field search) operations.
/// EventId range: 1040-1049
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class FindLog
{
    /// <summary>Logs when a find operation starts.</summary>
    [MessageLogging(
        EventId = 11229,
        Level = LogLevel.Information,
        Message = "Find started: searchTerm='{searchTerm}' container='{containerName}' fieldCount={fieldCount}")]
    public static partial IGenericMessage FindStarted(
        ILogger logger,
        string searchTerm,
        string containerName,
        int fieldCount);

    /// <summary>Logs when a find operation completes.</summary>
    [MessageLogging(
        EventId = 11230,
        Level = LogLevel.Information,
        Message = "Find completed: {resultCount} results in {elapsedMs:F1}ms")]
    public static partial IGenericMessage FindCompleted(
        ILogger logger,
        int resultCount,
        double elapsedMs);

    /// <summary>Logs when a find operation returns no results.</summary>
    [MessageLogging(
        EventId = 11231,
        Level = LogLevel.Information,
        Message = "Find returned no results for searchTerm='{searchTerm}' in container='{containerName}'")]
    public static partial IGenericMessage FindNoResults(
        ILogger logger,
        string searchTerm,
        string containerName);

    /// <summary>Logs when a find operation fails.</summary>
    [MessageLogging(
        EventId = 91023,
        Level = LogLevel.Error,
        Message = "Find failed: {errorMessage}")]
    public static partial IGenericMessage FindFailed(
        ILogger logger,
        Exception ex,
        string errorMessage);
}
