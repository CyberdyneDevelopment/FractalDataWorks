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
    /// Logs when the PermissionClaimsPreProcessor denies a request because the required
    /// permission is absent from the token's baked <c>perm</c> claims.
    /// </summary>
    // Why: EventId 8016 — next available in the 8001-8097 API Endpoints block.
    [MessageLogging(
        EventId = 51001,
        Level = LogLevel.Warning,
        Message = "Permission denied by PreProcessor for userId='{userId}': required permission '{requiredPermission}' not in baked perm claims")]
    public static partial IGenericMessage PermissionDeniedByPreProcessor(
        ILogger logger,
        string userId,
        string requiredPermission);
}
