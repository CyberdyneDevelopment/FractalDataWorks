using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Types.MsSql.Logging;

/// <summary>
/// MessageLogging for <see cref="MsSqlTypesProvider"/> query and persistence operations.
/// EventId range: 4404-4409
/// </summary>
[MessageLoggingTypeCode("TYPES")]
public static partial class MsSqlTypesProviderLog
{
    /// <summary>
    /// Logs when querying TypeCollection rows fails.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to query TypeCollection rows: {errorMessage}")]
    public static partial IGenericMessage GetCollectionsFailed(ILogger logger, Exception ex, string errorMessage);

    /// <summary>
    /// Logs when querying a single TypeCollection by name fails.
    /// </summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to query TypeCollection by name: {errorMessage}")]
    public static partial IGenericMessage GetCollectionFailed(ILogger logger, Exception ex, string errorMessage);

    /// <summary>
    /// Logs when querying TypeOption rows for a collection fails.
    /// </summary>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to query TypeOption rows for collection {collectionId}: {errorMessage}")]
    public static partial IGenericMessage GetOptionsFailed(ILogger logger, Exception ex, int collectionId, string errorMessage);

    /// <summary>
    /// Logs when persisting a TypeCollection record fails.
    /// </summary>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to save TypeCollection '{collectionName}': {errorMessage}")]
    public static partial IGenericMessage SaveCollectionFailed(ILogger logger, Exception ex, string collectionName, string errorMessage);

    /// <summary>
    /// Logs when persisting a TypeOption record fails.
    /// </summary>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Failed to save TypeOption '{optionName}': {errorMessage}")]
    public static partial IGenericMessage SaveOptionFailed(ILogger logger, Exception ex, string optionName, string errorMessage);
}
