using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.DataStores.Rest.Logging;

/// <summary>
/// Static logger class for REST schema importer messages (OpenAPI and OData).
/// </summary>
[MessageLoggingTypeCode("REST")]
public static partial class RestImporterLogger
{
    // OpenAPI Events: 2100-2119
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Starting OpenAPI schema import from: {source}")]
    public static partial IGenericMessage OpenApiImportStarted(ILogger logger, string source);

    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Completed OpenAPI schema import for API '{apiName}': {endpointCount} endpoints imported")]
    public static partial IGenericMessage OpenApiImportCompleted(ILogger logger, string apiName, int endpointCount);

    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "OpenAPI schema import failed")]
    public static partial IGenericMessage OpenApiImportFailed(ILogger logger, Exception exception);

    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Fetched OpenAPI spec from {source} ({size} bytes)")]
    public static partial IGenericMessage OpenApiFetched(ILogger logger, string source, long size);

    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to fetch OpenAPI spec from {source}")]
    public static partial IGenericMessage OpenApiFetchFailed(ILogger logger, Exception exception, string source);

    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Failed to parse OpenAPI spec: {errors}")]
    public static partial IGenericMessage OpenApiParsingFailed(ILogger logger, string errors);

    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Parsed endpoint {endpointId} with {fieldCount} fields")]
    public static partial IGenericMessage OpenApiEndpointParsed(ILogger logger, string endpointId, int fieldCount);

    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Excluded endpoint {endpointId} based on filter patterns")]
    public static partial IGenericMessage OpenApiEndpointExcluded(ILogger logger, string endpointId);

    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Reached maximum endpoint limit of {maxEndpoints}")]
    public static partial IGenericMessage OpenApiMaxEndpointsReached(ILogger logger, int maxEndpoints);

    // OData Events: 2120-2139
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Starting OData schema import from: {source}")]
    public static partial IGenericMessage ODataImportStarted(ILogger logger, string source);

    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Completed OData schema import for service '{serviceName}': {entitySetCount} EntitySets imported")]
    public static partial IGenericMessage ODataImportCompleted(ILogger logger, string serviceName, int entitySetCount);

    [MessageLogging(
        EventId = 70003,
        Level = LogLevel.Error,
        Message = "OData schema import failed")]
    public static partial IGenericMessage ODataImportFailed(ILogger logger, Exception exception);

    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Fetched OData $metadata from {metadataUrl} ({size} bytes)")]
    public static partial IGenericMessage ODataMetadataFetched(ILogger logger, string metadataUrl, long size);

    [MessageLogging(
        EventId = 70000,
        Level = LogLevel.Error,
        Message = "Failed to fetch OData $metadata from {metadataUrl}")]
    public static partial IGenericMessage ODataMetadataFetchFailed(ILogger logger, Exception exception, string metadataUrl);

    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to parse OData $metadata XML")]
    public static partial IGenericMessage ODataParsingFailed(ILogger logger, Exception exception);

    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Parsed EntitySet {entitySetName} with {fieldCount} fields")]
    public static partial IGenericMessage ODataEntitySetParsed(ILogger logger, string entitySetName, int fieldCount);

    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Excluded EntitySet {entitySetName} based on filter patterns")]
    public static partial IGenericMessage ODataEntitySetExcluded(ILogger logger, string entitySetName);

    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Reached maximum EntitySet limit of {maxEntitySets}")]
    public static partial IGenericMessage ODataMaxEntitySetsReached(ILogger logger, int maxEntitySets);

    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "EntityType not found for EntitySet {entitySetName}: {entityTypeName}")]
    public static partial IGenericMessage ODataEntityTypeNotFound(ILogger logger, string entitySetName, string entityTypeName);

    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Error processing EntitySet {entitySetName}")]
    public static partial IGenericMessage ODataEntitySetError(ILogger logger, Exception exception, string entitySetName);

    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Warning,
        Message = "Skipping EntitySet parse failure: {error}")]
    public static partial IGenericMessage ODataEntitySetSkipped(ILogger logger, string error);
}
