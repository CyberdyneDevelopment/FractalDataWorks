using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Fdw.Collections.Attributes;
using Fdw.Data.DataStores.Rest.Logging;
using Fdw.Data.DataStores.Rest.Results;
using Fdw.Data.SchemaImporters.Abstractions;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Imports schema from OData $metadata endpoints.
/// Returns a discovered <see cref="DataStoreConfiguration"/> with HttpPath rows containing
/// one Endpoint container per EntitySet.
/// </summary>
[TypeOption(typeof(SchemaImporters.Abstractions.SchemaImporters), "OData", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed partial class ODataSchemaImporter : SchemaImporterBase<RestConfiguration>, ISchemaImporter<RestConfiguration>
{
    private readonly ILogger<ODataSchemaImporter> _logger;

    // OData EDMX namespaces
    private static readonly XNamespace EdmxNamespace = "http://docs.oasis-open.org/odata/ns/edmx";
    private static readonly XNamespace EdmNamespaceV4 = "http://docs.oasis-open.org/odata/ns/edm";
    private static readonly XNamespace EdmNamespaceV3 = "http://schemas.microsoft.com/ado/2009/11/edm";

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataSchemaImporter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ODataSchemaImporter(ILogger<ODataSchemaImporter> logger)
        : base(
            id: 3,
            name: "OData",
            description: "Imports schema from OData $metadata endpoints",
            dataStoreType: "Rest")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region ISchemaImporter Implementation
    /// <summary>
    /// Imports schema from the specified OData service URL.
    /// </summary>
    /// <param name="source">The OData service URL.</param>
    /// <param name="options">The schema importer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public override async Task<IGenericResult<DataStoreConfiguration>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source))
                return GenericResult<DataStoreConfiguration>.Failure(
                    RestDataStoreResultCodes.ByName("ODataServiceUrlRequired"));

            RestImporterLogger.ODataImportStarted(_logger, source);

            var (baseUrl, metadataUrl) = NormalizeMetadataUrl(source);

            var metadataXmlResult = await FetchMetadata(metadataUrl, cancellationToken).ConfigureAwait(false);
            if (!metadataXmlResult.IsSuccess)
                return metadataXmlResult.ToNewResult<DataStoreConfiguration>();

            var parseResult = ParseEdmxDocument(metadataXmlResult.Value!);
            if (!parseResult.IsSuccess)
                return parseResult.ToNewResult<DataStoreConfiguration>();

            var edmx = parseResult.Value!;
            var edmNamespace = DetectEdmNamespace(edmx);
            var serviceName = edmx.Descendants(edmNamespace + "EntityContainer")
                .FirstOrDefault()?.Attribute("Name")?.Value ?? "OData Service";

            var dataStore = new DataStoreConfiguration
            {
                Name = serviceName,
                ServiceType = "DataStore",
                ServiceOptionType = "Rest",
                SectionName = "DataStores"
            };

            var totalEntitySets = 0;
            foreach (var pathResult in ParseEntitySets(edmx, edmNamespace, baseUrl, options))
            {
                if (!pathResult.IsSuccess)
                {
                    return pathResult.ToNewResult<DataStoreConfiguration>();
                }

                if (pathResult.Value is { } path)
                {
                    dataStore.Paths.Add(path);
                    totalEntitySets++;
                }
                else
                {
                    RestImporterLogger.ODataEntitySetSkipped(_logger, pathResult.CurrentMessage ?? string.Empty);
                }
            }

            RestImporterLogger.ODataImportCompleted(_logger, serviceName, totalEntitySets);

            return GenericResult<DataStoreConfiguration>.Success(dataStore);
        }
        catch (Exception ex)
        {
            return GenericResult<DataStoreConfiguration>.Failure(
                RestImporterLogger.ODataImportFailed(_logger, ex));
        }
    }

    private static (string BaseUrl, string MetadataUrl) NormalizeMetadataUrl(string source)
    {
        var baseUrl = source.TrimEnd('/');
        var metadataUrl = baseUrl;

        if (!metadataUrl.EndsWith("$metadata", StringComparison.OrdinalIgnoreCase))
        {
            metadataUrl += "/$metadata";
        }
        else
        {
            baseUrl = baseUrl.Substring(0, baseUrl.Length - "$metadata".Length).TrimEnd('/');
        }

        return (baseUrl, metadataUrl);
    }

    private IGenericResult<XDocument> ParseEdmxDocument(string metadataXml)
    {
        try
        {
            return GenericResult<XDocument>.Success(XDocument.Parse(metadataXml));
        }
        catch (Exception ex)
        {
            return GenericResult<XDocument>.Failure(
                RestImporterLogger.ODataParsingFailed(_logger, ex));
        }
    }

    /// <summary>
    /// Validates the OData service URL by checking accessibility of the $metadata endpoint.
    /// </summary>
    /// <param name="source">The OData service URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public override async Task<IGenericResult<bool>> Validate(
        string source,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source))
                return GenericResult<bool>.Failure(RestDataStoreResultCodes.ByName("ODataServiceUrlRequired"));

            // Ensure URL ends with $metadata
            var metadataUrl = source.TrimEnd('/');
            if (!metadataUrl.EndsWith("$metadata", StringComparison.OrdinalIgnoreCase))
            {
                metadataUrl += "/$metadata";
            }

            // Validate $metadata endpoint is accessible
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var response = await httpClient.GetAsync(metadataUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            return GenericResult<bool>.Success(response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                RestDataStoreResultCodes.ByName("InvalidODataSource"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    #endregion

    #region EntitySet Path Creation

    private List<IGenericResult<DataPathConfiguration>> ParseEntitySets(
        XDocument edmx,
        XNamespace edmNamespace,
        string baseUrl,
        SchemaImporterOptions? options)
    {
        var results = new List<IGenericResult<DataPathConfiguration>>();

        // Find all EntitySets
        var entitySets = edmx.Descendants(edmNamespace + "EntitySet").ToList();

        // Build a lookup of EntityType definitions
        var entityTypes = edmx.Descendants(edmNamespace + "EntityType")
            .ToDictionary(
                et => et.Attribute("Name")?.Value ?? string.Empty,
                et => et,
                StringComparer.Ordinal);

        foreach (var entitySet in entitySets)
        {
            var entitySetName = entitySet.Attribute("Name")?.Value;
            var entityTypeName = entitySet.Attribute("EntityType")?.Value;

            if (string.IsNullOrEmpty(entitySetName) || string.IsNullOrEmpty(entityTypeName))
                continue;

            // Apply filters
            if (ShouldExclude(entitySetName, options))
            {
                RestImporterLogger.ODataEntitySetExcluded(_logger, entitySetName);
                continue;
            }

            // Check max containers limit
            if (options?.MaxContainers.HasValue == true && results.Count >= options.MaxContainers.Value)
            {
                RestImporterLogger.ODataMaxEntitySetsReached(_logger, options.MaxContainers.Value);
                break;
            }

            // Extract the simple type name (remove namespace prefix)
            var simpleTypeName = entityTypeName.Contains('.')
                ? entityTypeName.Substring(entityTypeName.LastIndexOf('.') + 1)
                : entityTypeName;

            // Find EntityType definition
            if (!entityTypes.TryGetValue(simpleTypeName, out var entityType))
            {
                RestImporterLogger.ODataEntityTypeNotFound(_logger, entitySetName, entityTypeName);
                continue;
            }

            var pathResult = CreateEntitySetPath(baseUrl, entitySetName, entityType, edmNamespace, options);
            results.Add(pathResult);
        }

        return results;
    }

    private IGenericResult<DataPathConfiguration> CreateEntitySetPath(
        string baseUrl,
        string entitySetName,
        XElement entityType,
        XNamespace edmNamespace,
        SchemaImporterOptions? options)
    {
        try
        {
            // 1. Build the endpoint container config with its discovered fields.
            //    OData EntitySets support CRUD operations; the container type discriminator is Endpoint.
            var container = new DataContainerConfiguration
            {
                Id = Guid.NewGuid(),
                Name = entitySetName,
                TypeId = "Endpoint",
                Format = "Json"
            };

            var ordinal = 0;
            foreach (var field in BuildFieldsFromEntityType(entityType, edmNamespace))
            {
                field.Ordinal = ordinal;
                container.Fields.Add(field);
                ordinal++;
            }

            // 2. Build the path config holding the entity-set container
            var path = new DataPathConfiguration
            {
                Id = Guid.NewGuid(),
                Name = entitySetName,
                PathName = $"{baseUrl.TrimEnd('/')}/{entitySetName}",
                PathType = "HttpPath"
            };
            path.Containers.Add(container);

            LogEntitySetParsed(_logger, entitySetName, container.Fields.Count);

            return GenericResult<DataPathConfiguration>.Success(path);
        }
        catch (Exception ex)
        {
            LogEntitySetError(_logger, entitySetName, ex);
            return GenericResult<DataPathConfiguration>.Failure(
                RestDataStoreResultCodes.ByName("ODataEntitySetPathFailed"),
                ResultDetails.Create()
                    .With("EntitySetName", entitySetName)
                    .With("ErrorMessage", ex.Message));
        }
    }

    #endregion

    #region Schema Building

    private static List<DataContainerFieldConfiguration> BuildFieldsFromEntityType(XElement entityType, XNamespace edmNamespace)
    {
        var fields = new List<DataContainerFieldConfiguration>();

        // Parse properties
        var properties = entityType.Descendants(edmNamespace + "Property").ToList();

        foreach (var property in properties)
        {
            var propertyName = property.Attribute("Name")?.Value;
            var propertyType = property.Attribute("Type")?.Value;
            var nullable = property.Attribute("Nullable")?.Value;

            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyType))
                continue;

            var isNullable = string.Equals(nullable, "true", StringComparison.OrdinalIgnoreCase);

            fields.Add(new DataContainerFieldConfiguration
            {
                Id = Guid.NewGuid(),
                Name = propertyName,
                DataType = MapEdmTypeName(propertyType),
                IsNullable = isNullable
            });
        }

        return fields;
    }

    private static string MapEdmTypeName(string edmType)
    {
        // Why: strip the "Edm." namespace prefix OData services emit before looking up in the converter collection.
        var typeName = edmType.StartsWith("Edm.", StringComparison.Ordinal)
            ? edmType.Substring(4)
            : edmType;

        var converter = ODataConverters.BySourceType(typeName);
        // Why: check the NotFound sentinel, never null — TypeCollection lookups return sentinel on miss.
        if (converter == ODataConverters.NotFound)
            return typeof(object).Name;

        return converter.TargetClrType.Name;
    }

    #endregion

    #region Helper Methods

    private async Task<IGenericResult<string>> FetchMetadata(string metadataUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            var content = await httpClient.GetStringAsync(metadataUrl, cancellationToken).ConfigureAwait(false);
            LogMetadataFetched(_logger, metadataUrl, content.Length);
            return GenericResult<string>.Success(content);
        }
        catch (Exception ex)
        {
            LogFetchFailed(_logger, metadataUrl, ex);
            return GenericResult<string>.Failure(
                RestDataStoreResultCodes.ByName("ODataMetadataFetchFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    private static XNamespace DetectEdmNamespace(XDocument edmx)
    {
        // Try v4 first
        if (edmx.Descendants(EdmNamespaceV4 + "EntityType").Any())
            return EdmNamespaceV4;

        // Fallback to v3
        if (edmx.Descendants(EdmNamespaceV3 + "EntityType").Any())
            return EdmNamespaceV3;

        // Default to v4
        return EdmNamespaceV4;
    }

    private static bool ShouldExclude(string containerName, SchemaImporterOptions? options)
    {
        // Check include schemas (treat as include patterns for OData)
        if (options?.IncludeSchemas != null && options.IncludeSchemas.Any())
        {
            var matches = options.IncludeSchemas.Any(pattern =>
                containerName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            if (!matches)
                return true;
        }

        // Check exclude schemas (treat as exclude patterns for OData)
        if (options?.ExcludeSchemas != null && options.ExcludeSchemas.Any())
        {
            var matches = options.ExcludeSchemas.Any(pattern =>
                containerName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            if (matches)
                return true;
        }

        return false;
    }

    #endregion

    #region Logging

    [LoggerMessage(EventId = 300, Level = LogLevel.Information, Message = "Starting OData schema import from: {Source}")]
    private static partial void LogImportStarted(ILogger logger, string source);

    [LoggerMessage(EventId = 301, Level = LogLevel.Information, Message = "Completed OData schema import for service '{ServiceName}': {EntitySetCount} EntitySets imported")]
    private static partial void LogImportCompleted(ILogger logger, string serviceName, int entitySetCount);

    [LoggerMessage(EventId = 302, Level = LogLevel.Error, Message = "OData schema import failed")]
    private static partial void LogImportFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 303, Level = LogLevel.Information, Message = "Fetched OData $metadata from {MetadataUrl} ({Size} bytes)")]
    private static partial void LogMetadataFetched(ILogger logger, string metadataUrl, int size);

    [LoggerMessage(EventId = 304, Level = LogLevel.Error, Message = "Failed to fetch OData $metadata from {MetadataUrl}")]
    private static partial void LogFetchFailed(ILogger logger, string metadataUrl, Exception exception);

    [LoggerMessage(EventId = 305, Level = LogLevel.Error, Message = "Failed to parse OData $metadata XML")]
    private static partial void LogParsingFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 306, Level = LogLevel.Debug, Message = "Parsed EntitySet {EntitySetName} with {FieldCount} fields")]
    private static partial void LogEntitySetParsed(ILogger logger, string entitySetName, int fieldCount);

    [LoggerMessage(EventId = 307, Level = LogLevel.Debug, Message = "Excluded EntitySet {EntitySetName} based on filter patterns")]
    private static partial void LogEntitySetExcluded(ILogger logger, string entitySetName);

    [LoggerMessage(EventId = 308, Level = LogLevel.Information, Message = "Reached maximum EntitySet limit of {MaxEntitySets}")]
    private static partial void LogMaxEntitySetsReached(ILogger logger, int maxEntitySets);

    [LoggerMessage(EventId = 309, Level = LogLevel.Warning, Message = "EntityType not found for EntitySet {EntitySetName}: {EntityTypeName}")]
    private static partial void LogEntityTypeNotFound(ILogger logger, string entitySetName, string entityTypeName);

    [LoggerMessage(EventId = 310, Level = LogLevel.Error, Message = "Error processing EntitySet {EntitySetName}")]
    private static partial void LogEntitySetError(ILogger logger, string entitySetName, Exception exception);

    #endregion
}
