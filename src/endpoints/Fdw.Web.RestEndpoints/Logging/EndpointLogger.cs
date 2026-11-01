using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.RestEndpoints.Logging;

/// <summary>
/// Static logger class for generic REST endpoint operations.
/// </summary>
[MessageLoggingTypeCode("RESTENDPOINTS")]
public static partial class EndpointLogger
{
    /// <summary>
    /// Logs when an error occurs in an endpoint.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="endpointType">The type name of the endpoint where the error occurred.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Error in endpoint {endpointType}")]
    public static partial IGenericMessage EndpointError(
        ILogger logger,
        Exception exception,
        string endpointType);

    /// <summary>
    /// Logs when a command encounters an invalid operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The invalid operation exception.</param>
    /// <param name="commandType">The type name of the command.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "Invalid operation in command {commandType}")]
    public static partial IGenericMessage InvalidOperation(
        ILogger logger,
        InvalidOperationException exception,
        string commandType);

    /// <summary>
    /// Logs when a command receives an invalid argument.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The argument exception.</param>
    /// <param name="commandType">The type name of the command.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Invalid argument in command {commandType}")]
    public static partial IGenericMessage InvalidArgument(
        ILogger logger,
        ArgumentException exception,
        string commandType);

    /// <summary>
    /// Logs when an endpoint execution begins.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <param name="requestType">The type name of the request.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Executing endpoint {endpointType} with request {requestType}")]
    public static partial IGenericMessage ExecutingEndpoint(
        ILogger logger,
        string endpointType,
        string requestType);

    /// <summary>
    /// Logs when an endpoint execution completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <param name="duration">The execution duration in milliseconds.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Endpoint {endpointType} executed successfully in {duration}ms")]
    public static partial IGenericMessage EndpointExecuted(
        ILogger logger,
        string endpointType,
        double duration);

    /// <summary>
    /// Logs when authorization fails for an endpoint.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <param name="reason">The reason for authorization failure.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "Authorization failed for endpoint {endpointType}: {reason}")]
    public static partial IGenericMessage AuthorizationFailed(
        ILogger logger,
        string endpointType,
        string reason);

    /// <summary>
    /// Logs when authorization succeeds for an endpoint.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Authorization succeeded for endpoint {endpointType}")]
    public static partial IGenericMessage AuthorizationSucceeded(
        ILogger logger,
        string endpointType);

    /// <summary>
    /// Logs when command validation fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type name of the command.</param>
    /// <param name="validationErrors">The validation error messages.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Warning,
        Message = "Command validation failed for {commandType}: {validationErrors}")]
    public static partial IGenericMessage CommandValidationFailed(
        ILogger logger,
        string commandType,
        string validationErrors);

    /// <summary>
    /// Logs when a command execution begins.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type name of the command.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Executing command {commandType}")]
    public static partial IGenericMessage ExecutingCommand(
        ILogger logger,
        string commandType);

    /// <summary>
    /// Logs when a command execution completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type name of the command.</param>
    /// <param name="duration">The execution duration in milliseconds.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Command {commandType} executed successfully in {duration}ms")]
    public static partial IGenericMessage CommandExecuted(
        ILogger logger,
        string commandType,
        double duration);

    /// <summary>
    /// Logs when a void command execution fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="commandType">The type name of the command.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Command {commandType} execution failed")]
    public static partial IGenericMessage CommandExecutionFailed(
        ILogger logger,
        string commandType);

    /// <summary>
    /// Logs when a resource is not found for deletion.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="resourceType">The type name of the resource.</param>
    /// <param name="resourceName">The name of the resource that was not found.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "{resourceType} '{resourceName}' not found")]
    public static partial IGenericMessage ResourceNotFound(
        ILogger logger,
        string resourceType,
        string resourceName);

    /// <summary>
    /// Logs when operation dispatch fails after trigger acceptance.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Dispatch failed for trigger '{resourceName}'")]
    public static partial IGenericMessage DispatchFailed(
        ILogger logger,
        string resourceName);

    /// <summary>
    /// Logs when a paginated list request is received.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The effective take value.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Trace,
        Message = "Paginated list request on {endpointType}: skip={skip}, take={take}")]
    public static partial IGenericMessage PaginatedListRequest(
        ILogger logger,
        string endpointType,
        int skip,
        int take);

    /// <summary>
    /// Logs when a paginated list response is sent.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <param name="itemCount">The number of items returned.</param>
    /// <param name="totalCount">The total number of items available.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Trace,
        Message = "Paginated list response from {endpointType}: returned={itemCount}, total={totalCount}")]
    public static partial IGenericMessage PaginatedListResponse(
        ILogger logger,
        string endpointType,
        int itemCount,
        int totalCount);

    /// <summary>
    /// Logs that a paging setting a list request needs resolved to nothing positive.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="settingName">The setting that is missing, e.g. <c>DefaultPaginationSize</c>.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Error,
        Message = "Paginated list refused: setting '{settingName}' is missing or not a positive number; declare it as a server setting")]
    public static partial IGenericMessage PaginationSettingMissing(
        ILogger logger,
        string settingName);

    /// <summary>
    /// Logs that no settings provider is registered, so no paging setting can be read.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="serviceName">The service that is not registered.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Error,
        Message = "Paginated list refused: '{serviceName}' is not registered, so the paging settings cannot be read")]
    public static partial IGenericMessage PaginationSettingsUnavailable(
        ILogger logger,
        string serviceName);

    /// <summary>
    /// Logs that a paging query parameter is not a number in its allowed range.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="parameter">The query parameter, <c>skip</c> or <c>take</c>.</param>
    /// <param name="value">The value the caller sent.</param>
    /// <param name="rule">What the value must be.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Warning,
        Message = "Paginated list refused: '{parameter}={value}' is invalid; it must be {rule}")]
    public static partial IGenericMessage PaginationValueInvalid(
        ILogger logger,
        string parameter,
        string value,
        string rule);

    /// <summary>
    /// Logs that a requested or default take was larger than the configured maximum and was capped.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="requestedTake">The take asked for, or the default when none was.</param>
    /// <param name="maxTake">The configured maximum it was capped to.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Trace,
        Message = "Paginated list take {requestedTake} capped to the configured maximum {maxTake}")]
    public static partial IGenericMessage PaginationTakeCapped(
        ILogger logger,
        int requestedTake,
        int maxTake);

    /// <summary>
    /// Logs that a paging setting an API-key caller needs is absent, not a whole number, or negative.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="settingName">The setting that is not usable, e.g. <c>ApiKeyMaxPaginationSize</c>.</param>
    /// <returns>A generic message containing the error information.</returns>
    /// <remarks>
    /// Refused rather than read as no limit: 0 is how an operator says no limit, so an absent setting must not be
    /// able to say it for them.
    /// </remarks>
    [MessageLogging(
        EventId = 61004,
        Level = LogLevel.Error,
        Message = "Paginated request from an API key refused: setting '{settingName}' is missing, not a whole number or negative; declare it as a server setting (0 means no limit)")]
    public static partial IGenericMessage ApiKeyPaginationSettingInvalid(
        ILogger logger,
        string settingName);

    /// <summary>
    /// Logs that an API-key caller's page was sized from the ApiKey paging settings.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="defaultTake">The resolved default page size; 0 means no limit.</param>
    /// <param name="maxTake">The resolved largest page size; 0 means no limit.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Trace,
        Message = "Paging for an API-key caller resolved from the ApiKey settings: default={defaultTake}, max={maxTake} (0 means no limit)")]
    public static partial IGenericMessage ApiKeyPaginationLimits(
        ILogger logger,
        int defaultTake,
        int maxTake);

    /// <summary>
    /// Logs that an API-key caller asked for no page size and no limit applies, so every row is wanted.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skip">The offset every row is returned from.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Trace,
        Message = "API-key caller named no take and no limit applies: every row from {skip} on is returned")]
    public static partial IGenericMessage ApiKeyPaginationUnlimited(
        ILogger logger,
        int skip);

    /// <summary>
    /// Logs that a data query is answered by reading every row in batches as the response is written.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="dataSetName">The data set read.</param>
    /// <param name="batchSize">The rows read per batch.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Trace,
        Message = "Data set '{dataSetName}' is streamed whole, {batchSize} rows to a batch")]
    public static partial IGenericMessage DataQueryStreamOpened(
        ILogger logger,
        string dataSetName,
        int batchSize);

    /// <summary>
    /// Logs that every row of a streamed data query was written.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="dataSetName">The data set read.</param>
    /// <param name="rows">How many rows were written.</param>
    /// <returns>A generic message containing the information.</returns>
    [MessageLogging(
        EventId = 11043,
        Level = LogLevel.Information,
        Message = "Data set '{dataSetName}' streamed whole: {rows} rows written")]
    public static partial IGenericMessage DataQueryStreamCompleted(
        ILogger logger,
        string dataSetName,
        int rows);

    /// <summary>
    /// Logs that a batch of a streamed data query failed after rows had been written, so the response is aborted.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="dataSetName">The data set read.</param>
    /// <param name="rows">How many rows had been written before the failure.</param>
    /// <returns>A generic message containing the warning information.</returns>
    /// <remarks>The failed batch's own message was logged where it failed; this says what became of the response.</remarks>
    [MessageLogging(
        EventId = 11044,
        Level = LogLevel.Warning,
        Message = "Data set '{dataSetName}' stream failed after {rows} rows; the response is aborted so it cannot pass for a whole one")]
    public static partial IGenericMessage DataQueryStreamFailed(
        ILogger logger,
        string dataSetName,
        int rows);

    /// <summary>
    /// Logs when the PermissionClaimsPreProcessor denies a request because the required
    /// permission is absent from the token's baked <c>perm</c> claims. Its number is in the
    /// forbidden band, so the answer is a 403, and it carries the request's reference so the
    /// journal line is found from the referenceId the caller was given.
    /// </summary>
    [MessageLogging(
        EventId = 101000,
        Level = LogLevel.Warning,
        Message = "Permission denied by PreProcessor for userId='{userId}' (reference '{referenceId}'): required permission '{requiredPermission}' not in baked perm claims",
        ClientMessage = "This needs the permission '{requiredPermission}', which you do not hold.")]
    public static partial IGenericMessage PermissionDeniedByPreProcessor(
        ILogger logger,
        string userId,
        string requiredPermission,
        string referenceId);

    /// <summary>
    /// Logs that a streamed page of a list was sent in full.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <param name="skip">The page's offset.</param>
    /// <param name="itemCount">How many items the page carried.</param>
    /// <param name="hasMore">Whether another page follows.</param>
    /// <returns>A generic message containing the trace information.</returns>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Trace,
        Message = "Streamed page from {endpointType}: skip={skip}, returned={itemCount}, hasMore={hasMore}")]
    public static partial IGenericMessage StreamedPageSent(
        ILogger logger,
        string endpointType,
        int skip,
        int itemCount,
        bool hasMore);

    /// <summary>
    /// Logs that a streamed page ended early because an item could not be produced. The status code
    /// was already sent, so the cause travels in the page's error trailer.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="endpointType">The type name of the endpoint.</param>
    /// <param name="itemCount">How many items were sent before the page ended.</param>
    /// <param name="reason">Why the page ended.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Streamed page from {endpointType} ended after {itemCount} item(s): {reason}")]
    public static partial IGenericMessage StreamedPageEnded(
        ILogger logger,
        string endpointType,
        int itemCount,
        string? reason);

    /// <summary>
    /// Logs that a query over a resource was refused because something in its body does not resolve.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="resource">The resource that was queried.</param>
    /// <param name="reason">The first thing in the query that did not resolve.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Warning,
        Message = "Query over {resource} refused: {reason}")]
    public static partial IGenericMessage QueryRefused(
        ILogger logger,
        string resource,
        string reason);

    /// <summary>
    /// Logs that a query over a resource cannot be answered because its record has no registered mapper,
    /// so the fields it declares cannot be known.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="resource">The resource that was queried.</param>
    /// <param name="recordType">The record type with no mapper.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 60001,
        Level = LogLevel.Error,
        Message = "Query over {resource} refused: no field mapper is registered for {recordType}")]
    public static partial IGenericMessage QueryFieldsUnavailable(
        ILogger logger,
        string resource,
        string recordType);

    /// <summary>
    /// Logs that a resource declares queryable fields that do not fit the record it queries, so no query
    /// over it can be answered until the declaration is corrected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="resource">The resource whose declaration is wrong.</param>
    /// <param name="reason">What is wrong with it.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 60004,
        Level = LogLevel.Error,
        Message = "Query over {resource} refused: its declared queryable fields are invalid: {reason}")]
    public static partial IGenericMessage QueryableFieldsInvalid(
        ILogger logger,
        string resource,
        string reason);
}
