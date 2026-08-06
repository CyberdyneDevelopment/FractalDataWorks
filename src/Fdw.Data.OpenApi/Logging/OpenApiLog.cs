using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.OpenApi.Logging;

/// <summary>
/// Message logging for OpenAPI schema-driven command translators.
/// EventId range: 4350-4399
/// </summary>
[MessageLoggingTypeCode("OPENAPI")]
public static partial class OpenApiLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (4350-4359) - Detailed schema processing steps
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when starting to process an OpenAPI operation.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Processing OpenAPI operation '{operationId}' at '{path}'")]
    public static partial IGenericMessage ProcessingOperation(
        ILogger logger,
        string operationId,
        string path);

    /// <summary>
    /// Logs when parsing a parameter from the OpenAPI spec.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "Parsing parameter '{parameterName}' (in: {location}, type: {parameterType})")]
    public static partial IGenericMessage ParsingParameter(
        ILogger logger,
        string parameterName,
        string location,
        string parameterType);

    /// <summary>
    /// Logs when processing request body schema.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Processing request body schema: {contentType}")]
    public static partial IGenericMessage ProcessingRequestBody(
        ILogger logger,
        string contentType);

    /// <summary>
    /// Logs when processing response schema.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "Processing response schema for status {statusCode}: {contentType}")]
    public static partial IGenericMessage ProcessingResponse(
        ILogger logger,
        string statusCode,
        string contentType);

    /// <summary>
    /// Logs when resolving a schema reference.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "Resolving schema reference: {reference}")]
    public static partial IGenericMessage ResolvingReference(
        ILogger logger,
        string reference);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug (4360-4369) - Translation decisions and intermediate results
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when building HTTP request from OpenAPI operation.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Building {httpMethod} request for '{operationId}'")]
    public static partial IGenericMessage BuildingRequest(
        ILogger logger,
        string httpMethod,
        string operationId);

    /// <summary>
    /// Logs when mapping a data command field to an OpenAPI parameter.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Mapped field '{fieldName}' to OpenAPI parameter '{parameterName}'")]
    public static partial IGenericMessage FieldMappedToParameter(
        ILogger logger,
        string fieldName,
        string parameterName);

    /// <summary>
    /// Logs the generated URL with path parameters.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Generated URL: {url}")]
    public static partial IGenericMessage UrlGenerated(
        ILogger logger,
        string url);

    /// <summary>
    /// Logs when serializing request body.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Debug,
        Message = "Serializing request body as {contentType} ({byteCount} bytes)")]
    public static partial IGenericMessage SerializingBody(
        ILogger logger,
        string contentType,
        int byteCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Information (4370-4379) - Key translation events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when OpenAPI specification is loaded.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Loaded OpenAPI spec '{title}' v{version} with {operationCount} operations")]
    public static partial IGenericMessage SpecLoaded(
        ILogger logger,
        string title,
        string version,
        int operationCount);

    /// <summary>
    /// Logs successful translation of a data command to HTTP request.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Translated {commandType} to OpenAPI operation '{operationId}'")]
    public static partial IGenericMessage TranslationCompleted(
        ILogger logger,
        string commandType,
        string operationId);

    /// <summary>
    /// Logs when an operation is selected based on command type.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Selected operation '{operationId}' for {commandType} on '{resourceName}'")]
    public static partial IGenericMessage OperationSelected(
        ILogger logger,
        string operationId,
        string commandType,
        string resourceName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warning (4380-4389) - Translation issues that don't prevent completion
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a required parameter is missing from the command.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Required parameter '{parameterName}' not found in command - using default")]
    public static partial IGenericMessage MissingRequiredParameter(
        ILogger logger,
        string parameterName);

    /// <summary>
    /// Logs when schema validation produces warnings.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Warning,
        Message = "Schema validation warning for '{propertyName}': {warning}")]
    public static partial IGenericMessage SchemaValidationWarning(
        ILogger logger,
        string propertyName,
        string warning);

    /// <summary>
    /// Logs when using deprecated operation.
    /// </summary>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "Using deprecated operation '{operationId}'")]
    public static partial IGenericMessage DeprecatedOperation(
        ILogger logger,
        string operationId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (4390-4399) - Translation failures
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when OpenAPI spec parsing fails.
    /// </summary>
    [MessageLogging(
        EventId = 90003,
        Level = LogLevel.Error,
        Message = "Failed to parse OpenAPI specification: {reason}")]
    public static partial IGenericMessage SpecParsingFailed(
        ILogger logger,
        string reason);

    /// <summary>
    /// Logs when no matching operation is found for a command.
    /// </summary>
    [MessageLogging(
        EventId = 30000,
        Level = LogLevel.Error,
        Message = "No matching OpenAPI operation found for {commandType} on '{resourceName}'")]
    public static partial IGenericMessage NoMatchingOperation(
        ILogger logger,
        string commandType,
        string resourceName);

    /// <summary>
    /// Logs when schema validation fails.
    /// </summary>
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "Schema validation failed for '{propertyName}': {error}")]
    public static partial IGenericMessage SchemaValidationFailed(
        ILogger logger,
        string propertyName,
        string error);

    /// <summary>
    /// Logs when translation fails with an exception.
    /// </summary>
    [MessageLogging(
        EventId = 90000,
        Level = LogLevel.Error,
        Message = "OpenAPI translation exception for operation '{operationId}'")]
    public static partial IGenericMessage TranslationException(
        ILogger logger,
        Exception ex,
        string operationId);
}
