using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Api.OpenApi.Logging;

/// <summary>
/// MessageLogging for OpenAPI document processors.
/// EventId range: 4300-4310
/// </summary>
[MessageLoggingTypeCode("API")]
public static partial class OpenApiProcessorLog
{
    /// <summary>
    /// Logs when an unauthenticated user triggers public-only filtering.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Permission filter: unauthenticated user, filtering to public-only endpoints")]
    public static partial IGenericMessage FilteredToPublicOnly(ILogger logger);

    /// <summary>
    /// Logs when an admin user bypasses operation filtering.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Permission filter: admin user, showing all {operationCount} operations")]
    public static partial IGenericMessage AdminUserShowAll(ILogger logger, int operationCount);

    /// <summary>
    /// Logs the result of permission-based operation filtering for an authenticated user.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Permission filter: removed {removedCount} operations, {remainingCount} remaining for user with {permissionCount} permissions")]
    public static partial IGenericMessage FilteredOperations(ILogger logger, int removedCount, int remainingCount, int permissionCount);

    /// <summary>
    /// Traces removal of a single operation due to missing permission.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "Permission filter: removed operation '{operationId}' on path '{path}' requiring policy '{policy}'")]
    public static partial IGenericMessage RemovedOperation(ILogger logger, string operationId, string path, string policy);
}
