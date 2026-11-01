using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.DataStores.Rest.Logging;
using Fdw.Data.DataStores.Rest.Results;
using Fdw.Data.JsonSchema;
using Fdw.Data.SchemaImporters.Abstractions;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Imports schema from OpenAPI 3.0/Swagger 2.0 specifications.
/// Returns a discovered <see cref="DataStoreImplementationConfiguration"/> with HttpPath rows containing
/// one Endpoint container per operation.
/// </summary>
[TypeOption(typeof(SchemaImporters.Abstractions.SchemaImporters), "OpenApi")]
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class RestOpenApiSchemaImporter : SchemaImporterBase<RestConfiguration>, ISchemaImporter<RestConfiguration>
{
    private readonly ILogger<RestOpenApiSchemaImporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestOpenApiSchemaImporter"/> class without a logger. The
    /// registration module initializer builds registered options through this constructor.
    /// </summary>
    public RestOpenApiSchemaImporter()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestOpenApiSchemaImporter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public RestOpenApiSchemaImporter(ILogger<RestOpenApiSchemaImporter>? logger)
        : base(
            name: "OpenApi",
            description: "Imports schema from OpenAPI 3.0/Swagger specifications",
            dataStoreType: "Rest")
    {
        _logger = logger ?? NullLogger<RestOpenApiSchemaImporter>.Instance;
    }

    #region ISchemaImporter Implementation

    /// <summary>
    /// Imports schema from an OpenAPI specification.
    /// </summary>
    /// <param name="source">The source URL or file path to the OpenAPI specification.</param>
    /// <param name="options">Optional import options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the discovered DataStore configuration or failure information.</returns>
    public override async Task<IGenericResult<DataStoreImplementationConfiguration>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source))
                return GenericResult<DataStoreImplementationConfiguration>.Failure(
                    RestDataStoreResultCodes.ByName("OpenApiSpecRequired"));

            RestImporterLogger.OpenApiImportStarted(_logger, source);

            var specContentResult = await FetchSpec(source, cancellationToken).ConfigureAwait(false);
            if (!specContentResult.IsSuccess)
                return specContentResult.ToNewResult<DataStoreImplementationConfiguration>();

            var parseResult = ParseOpenApiSpec(specContentResult.Value!);
            if (!parseResult.IsSuccess)
                return parseResult.ToNewResult<DataStoreImplementationConfiguration>();

            var openApiDocument = parseResult.Value!;
            var baseUrl = openApiDocument.Servers?.FirstOrDefault()?.Url ?? source;
            var storeName = openApiDocument.Info?.Title ?? "OpenAPI DataStore";

            var dataStore = new DataStoreImplementationConfiguration
            {
                Name = storeName,
            };

            var endpointsResult = ImportEndpoints(openApiDocument, baseUrl, dataStore, options);
            if (!endpointsResult.IsSuccess)
                return endpointsResult.ToNewResult<DataStoreImplementationConfiguration>();

            RestImporterLogger.OpenApiImportCompleted(_logger, storeName, endpointsResult.Value);

            return GenericResult<DataStoreImplementationConfiguration>.Success(dataStore);
        }
        catch (Exception ex)
        {
            return GenericResult<DataStoreImplementationConfiguration>.Failure(
                RestImporterLogger.OpenApiImportFailed(_logger, ex));
        }
    }

    private IGenericResult<OpenApiDocument> ParseOpenApiSpec(string specContent)
    {
        var reader = new OpenApiStringReader();
        var document = reader.Read(specContent, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            var errors = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
            return GenericResult<OpenApiDocument>.Failure(
                RestImporterLogger.OpenApiParsingFailed(_logger, errors));
        }

        return GenericResult<OpenApiDocument>.Success(document);
    }

    private IGenericResult<int> ImportEndpoints(
        OpenApiDocument document,
        string baseUrl,
        DataStoreImplementationConfiguration dataStore,
        SchemaImporterOptions? options)
    {
        var totalEndpoints = 0;

        foreach (var pathItem in document.Paths)
        {
            foreach (var operation in pathItem.Value.Operations)
            {
                var method = operation.Key.ToString().ToUpperInvariant();
                var pathTemplate = pathItem.Key;
                var containerId = $"{method} {pathTemplate}";

                if (ShouldExclude(containerId, options))
                {
                    RestImporterLogger.OpenApiEndpointExcluded(_logger, containerId);
                    continue;
                }

                if (options?.MaxContainers.HasValue == true && totalEndpoints >= options.MaxContainers.Value)
                {
                    RestImporterLogger.OpenApiMaxEndpointsReached(_logger, options.MaxContainers.Value);
                    break;
                }

                var pathResult = CreateEndpointPath(baseUrl, pathTemplate, operation.Key, operation.Value, options);
                if (!pathResult.IsSuccess)
                    return pathResult.ToNewResult<int>();

                dataStore.Paths.Add(pathResult.Value!);
                totalEndpoints++;
            }
        }

        return GenericResult<int>.Success(totalEndpoints);
    }

    /// <summary>
    /// Validates an OpenAPI specification source.
    /// </summary>
    /// <param name="source">The source URL or file path to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether the source is valid.</returns>
    public override async Task<IGenericResult<bool>> Validate(
        string source,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source))
                return GenericResult<bool>.Failure(RestDataStoreResultCodes.ByName("OpenApiSpecRequired"));

            // Check if URL or file path
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && (string.Equals(uri.Scheme, "http", StringComparison.Ordinal) || string.Equals(uri.Scheme, "https", StringComparison.Ordinal)))
            {
                // Validate HTTP URL is accessible
                using var httpClient = new HttpClient();
                httpClient.Timeout = SchemaImporters.Abstractions.SchemaImporters.SourceCheckTimeout;
                var response = await httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, source),
                    cancellationToken).ConfigureAwait(false);

                return GenericResult<bool>.Success(response.IsSuccessStatusCode);
            }
            else
            {
                // Validate file path exists
                var exists = File.Exists(source);
                return GenericResult<bool>.Success(exists);
            }
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                RestDataStoreResultCodes.ByName("InvalidOpenApiSource"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    #endregion

    #region Endpoint Path Creation

    private IGenericResult<DataPathConfiguration> CreateEndpointPath(
        string baseUrl,
        string pathTemplate,
        OperationType operationType,
        OpenApiOperation operation,
        SchemaImporterOptions? options)
    {
        try
        {
            var method = operationType.ToString().ToUpperInvariant();
            var operationId = operation.OperationId ?? $"{method}_{pathTemplate.Replace("/", "_")}";

            // 1. Build the endpoint container config with its discovered fields
            var container = new DataContainerConfiguration
            {
                Id = Guid.NewGuid(),
                Name = operationId,
                TypeId = "Endpoint",
                Format = DetermineFormatFromOperation(operation)
            };

            foreach (var fieldResult in BuildFieldsFromOperation(operation, $"{method} {pathTemplate}"))
            {
                if (!fieldResult.IsSuccess)
                    return fieldResult.ToNewResult<DataPathConfiguration>();

                fieldResult.Value!.Ordinal = container.Fields.Count;
                container.Fields.Add(fieldResult.Value);
            }

            // 2. Build the path config that holds the endpoint container
            var path = new DataPathConfiguration
            {
                Id = Guid.NewGuid(),
                Name = operationId,
                PathValue = $"{baseUrl.TrimEnd('/')}{pathTemplate}",
                PathType = "HttpPath",
                SourceDescription = string.IsNullOrEmpty(operation.Summary) ? operation.Description : operation.Summary
            };
            path.Containers.Add(container);

            RestImporterLogger.OpenApiEndpointParsed(_logger, $"{method} {pathTemplate}", container.Fields.Count);

            return GenericResult<DataPathConfiguration>.Success(path);
        }
        catch (Exception ex)
        {
            var method = operationType.ToString().ToUpperInvariant();
            return GenericResult<DataPathConfiguration>.Failure(
                RestDataStoreResultCodes.ByName("OpenApiEndpointPathFailed"),
                ResultDetails.Create()
                    .With("Method", method)
                    .With("PathTemplate", pathTemplate)
                    .With("ErrorMessage", ex.Message));
        }
    }

    #endregion

    #region Schema Building

    private static IEnumerable<IGenericResult<DataContainerFieldConfiguration>> BuildFieldsFromOperation(
        OpenApiOperation operation,
        string endpoint)
    {
        // Request body fields
        if (operation.RequestBody != null)
        {
            foreach (var content in operation.RequestBody.Content.Where(c => c.Value.Schema != null))
            {
                foreach (var field in ParseSchema(content.Value.Schema, "Request", $"{endpoint} request body"))
                    yield return field;
            }
        }

        // Response fields (200 OK response)
        if (operation.Responses.TryGetValue("200", out var successResponse))
        {
            foreach (var content in successResponse.Content.Where(c => c.Value.Schema != null))
            {
                foreach (var field in ParseSchema(content.Value.Schema, "Response", $"{endpoint} 200 response"))
                    yield return field;
            }
        }

        // Path parameters, then query parameters
        foreach (var param in operation.Parameters.Where(p => p.In == ParameterLocation.Path))
            yield return CreateFieldFromParameter(param, $"{endpoint} path parameter");

        foreach (var param in operation.Parameters.Where(p => p.In == ParameterLocation.Query))
            yield return CreateFieldFromParameter(param, $"{endpoint} query parameter");
    }

    private static IEnumerable<IGenericResult<DataContainerFieldConfiguration>> ParseSchema(
        OpenApiSchema schema,
        string prefix,
        string location)
    {
        if (string.Equals(schema.Type, "object", StringComparison.Ordinal) && schema.Properties != null)
        {
            foreach (var prop in schema.Properties)
            {
                yield return CreateField(
                    $"{prefix}.{prop.Key}",
                    location,
                    prop.Value.Type,
                    prop.Value.Format,
                    !(schema.Required?.Contains(prop.Key) ?? false),
                    prop.Value.Description);
            }
        }
        else if (string.Equals(schema.Type, "array", StringComparison.Ordinal) && schema.Items != null)
        {
            // For arrays, parse the items schema
            foreach (var field in ParseSchema(schema.Items, prefix, location))
                yield return field;
        }
    }

    private static IGenericResult<DataContainerFieldConfiguration> CreateFieldFromParameter(
        OpenApiParameter param,
        string location)
    {
        return CreateField(param.Name, location, param.Schema?.Type, param.Schema?.Format, !param.Required, param.Description);
    }

    private static IGenericResult<DataContainerFieldConfiguration> CreateField(
        string name,
        string location,
        string? jsonType,
        string? format,
        bool isNullable,
        string? description)
    {
        var dataType = MapJsonSchemaType(name, location, jsonType, format);
        if (!dataType.IsSuccess)
            return dataType.ToNewResult<DataContainerFieldConfiguration>();

        return GenericResult<DataContainerFieldConfiguration>.Success(new DataContainerFieldConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name,
            DataType = dataType.Value!.Name,
            IsNullable = isNullable,
            Description = description
        });
    }

    private static IGenericResult<Type> MapJsonSchemaType(string field, string location, string? jsonType, string? format)
    {
        if (string.IsNullOrEmpty(jsonType))
            return FieldTypeUnknown(field, location, "(no type declared)");

        // Build composite key: type+format (e.g., "integer+int64", "string+date-time")
        var sourceTypeName = jsonType.ToLowerInvariant();
        var compositeKey = string.IsNullOrEmpty(format)
            ? sourceTypeName
            : $"{sourceTypeName}+{format.ToLowerInvariant()}";

        // Look up converter using JsonSchemaConverters
        var converter = JsonSchemaConverters.BySourceType(compositeKey);

        // If not found with format, try without format
        if (converter == JsonSchemaConverters.NotFound && !string.IsNullOrEmpty(format))
        {
            converter = JsonSchemaConverters.BySourceType(sourceTypeName);
        }

        return converter == JsonSchemaConverters.NotFound
            ? FieldTypeUnknown(field, location, compositeKey)
            : GenericResult<Type>.Success(converter.TargetClrType);
    }

    private static IGenericResult<Type> FieldTypeUnknown(string field, string location, string sourceType)
    {
        return GenericResult<Type>.Failure(
            RestDataStoreResultCodes.ByName("OpenApiFieldTypeUnknown"),
            ResultDetails.Create()
                .With("Field", field)
                .With("Location", location)
                .With("SourceType", sourceType));
    }

    #endregion

    #region Format Detection

    private static string DetermineFormatFromOperation(OpenApiOperation operation)
    {
        // Check response content types
        if (operation.Responses.TryGetValue("200", out var successResponse))
        {
            foreach (var contentType in successResponse.Content.Keys)
            {
                if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                    return "Json";
                if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
                    return "Xml";
            }
        }

        // Check request body content types
        if (operation.RequestBody != null)
        {
            foreach (var contentType in operation.RequestBody.Content.Keys)
            {
                if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                    return "Json";
                if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
                    return "Xml";
            }
        }

        // Default to JSON
        return "Json";
    }

    #endregion

    #region Helper Methods

    private async Task<IGenericResult<string>> FetchSpec(string source, CancellationToken cancellationToken)
    {
        try
        {
            // Check if URL or file path
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && (string.Equals(uri.Scheme, "http", StringComparison.Ordinal) || string.Equals(uri.Scheme, "https", StringComparison.Ordinal)))
            {
                // Fetch from HTTP
                using var httpClient = new HttpClient();
                httpClient.Timeout = SchemaImporters.Abstractions.SchemaImporters.SourceFetchTimeout;
                var content = await httpClient.GetStringAsync(source, cancellationToken).ConfigureAwait(false);
                RestImporterLogger.OpenApiFetched(_logger, source, content.Length);
                return GenericResult<string>.Success(content);
            }
            else
            {
                // Read from file
                if (!File.Exists(source))
                    return GenericResult<string>.Failure(
                        RestDataStoreResultCodes.ByName("OpenApiFileNotFound"),
                        ResultDetails.Create().With("FilePath", source));

                var content = await File.ReadAllTextAsync(source, cancellationToken).ConfigureAwait(false);
                RestImporterLogger.OpenApiFetched(_logger, source, content.Length);
                return GenericResult<string>.Success(content);
            }
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                RestDataStoreResultCodes.ByName("OpenApiSpecFetchFailed"),
                ResultDetails.Create()
                    .With("Source", source)
                    .With("ErrorMessage", ex.Message));
        }
    }

    private static bool ShouldExclude(string containerName, SchemaImporterOptions? options)
    {
        // Check include schemas (treat as include patterns for REST)
        if (options?.IncludeSchemas != null && options.IncludeSchemas.Any())
        {
            var matches = options.IncludeSchemas.Any(pattern =>
                containerName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            if (!matches)
                return true;
        }

        // Check exclude schemas (treat as exclude patterns for REST)
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
}
