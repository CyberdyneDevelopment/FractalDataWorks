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
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Imports schema from OpenAPI 3.0/Swagger 2.0 specifications.
/// Returns a discovered <see cref="DataStoreConfiguration"/> with HttpPath rows containing
/// one Endpoint container per operation.
/// </summary>
[TypeOption(typeof(SchemaImporters.Abstractions.SchemaImporters), "OpenApi", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class RestOpenApiSchemaImporter : SchemaImporterBase<RestConfiguration>, ISchemaImporter<RestConfiguration>
{
    private readonly ILogger<RestOpenApiSchemaImporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestOpenApiSchemaImporter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public RestOpenApiSchemaImporter(ILogger<RestOpenApiSchemaImporter> logger)
        : base(
            id: 2,
            name: "OpenApi",
            description: "Imports schema from OpenAPI 3.0/Swagger specifications",
            dataStoreType: "Rest")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region ISchemaImporter Implementation

    /// <summary>
    /// Imports schema from an OpenAPI specification.
    /// </summary>
    /// <param name="source">The source URL or file path to the OpenAPI specification.</param>
    /// <param name="options">Optional import options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the discovered DataStore configuration or failure information.</returns>
    public override async Task<IGenericResult<DataStoreConfiguration>> Import(
        string source,
        SchemaImporterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source))
                return GenericResult<DataStoreConfiguration>.Failure(
                    RestDataStoreResultCodes.ByName("OpenApiSpecRequired"));

            RestImporterLogger.OpenApiImportStarted(_logger, source);

            var specContentResult = await FetchSpec(source, cancellationToken).ConfigureAwait(false);
            if (!specContentResult.IsSuccess)
                return specContentResult.ToNewResult<DataStoreConfiguration>();

            var parseResult = ParseOpenApiSpec(specContentResult.Value!);
            if (!parseResult.IsSuccess)
                return parseResult.ToNewResult<DataStoreConfiguration>();

            var openApiDocument = parseResult.Value!;
            var baseUrl = openApiDocument.Servers?.FirstOrDefault()?.Url ?? source;
            var storeName = openApiDocument.Info?.Title ?? "OpenAPI DataStore";

            var dataStore = new DataStoreConfiguration
            {
                Name = storeName,
                ServiceType = "DataStore",
                ServiceOptionType = "Rest",
                SectionName = "DataStores"
            };

            var totalEndpoints = ImportEndpoints(openApiDocument, baseUrl, dataStore, options);

            RestImporterLogger.OpenApiImportCompleted(_logger, storeName, totalEndpoints);

            return GenericResult<DataStoreConfiguration>.Success(dataStore);
        }
        catch (Exception ex)
        {
            return GenericResult<DataStoreConfiguration>.Failure(
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

    private int ImportEndpoints(
        OpenApiDocument document,
        string baseUrl,
        DataStoreConfiguration dataStore,
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

                if (pathResult.IsSuccess && pathResult.Value != null)
                {
                    dataStore.Paths.Add(pathResult.Value);
                    totalEndpoints++;
                }
            }
        }

        return totalEndpoints;
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
                httpClient.Timeout = TimeSpan.FromSeconds(10);
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

            var ordinal = 0;
            foreach (var field in BuildFieldsFromOperation(operation))
            {
                field.Ordinal = ordinal;
                container.Fields.Add(field);
                ordinal++;
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

    private static List<DataContainerFieldConfiguration> BuildFieldsFromOperation(OpenApiOperation operation)
    {
        var fields = new List<DataContainerFieldConfiguration>();

        // Add request body fields
        if (operation.RequestBody != null)
        {
            foreach (var content in operation.RequestBody.Content)
            {
                if (content.Value.Schema != null)
                {
                    fields.AddRange(ParseSchema(content.Value.Schema, "Request"));
                }
            }
        }

        // Add response fields (200 OK response)
        if (operation.Responses.TryGetValue("200", out var successResponse))
        {
            foreach (var content in successResponse.Content)
            {
                if (content.Value.Schema != null)
                {
                    fields.AddRange(ParseSchema(content.Value.Schema, "Response"));
                }
            }
        }

        // Add path parameters
        foreach (var param in operation.Parameters.Where(p => p.In == ParameterLocation.Path))
        {
            fields.Add(CreateFieldFromParameter(param));
        }

        // Add query parameters
        foreach (var param in operation.Parameters.Where(p => p.In == ParameterLocation.Query))
        {
            fields.Add(CreateFieldFromParameter(param));
        }

        return fields;
    }

    private static List<DataContainerFieldConfiguration> ParseSchema(OpenApiSchema schema, string prefix)
    {
        var fields = new List<DataContainerFieldConfiguration>();

        if (string.Equals(schema.Type, "object", StringComparison.Ordinal) && schema.Properties != null)
        {
            foreach (var prop in schema.Properties)
            {
                var isRequired = schema.Required?.Contains(prop.Key) ?? false;

                fields.Add(new DataContainerFieldConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = $"{prefix}.{prop.Key}",
                    DataType = MapJsonSchemaTypeName(prop.Value.Type, prop.Value.Format),
                    IsNullable = !isRequired,
                    Description = prop.Value.Description
                });
            }
        }
        else if (string.Equals(schema.Type, "array", StringComparison.Ordinal) && schema.Items != null)
        {
            // For arrays, parse the items schema
            fields.AddRange(ParseSchema(schema.Items, prefix));
        }

        return fields;
    }

    private static DataContainerFieldConfiguration CreateFieldFromParameter(OpenApiParameter param)
    {
        return new DataContainerFieldConfiguration
        {
            Id = Guid.NewGuid(),
            Name = param.Name,
            DataType = MapJsonSchemaTypeName(param.Schema?.Type, param.Schema?.Format),
            IsNullable = !param.Required,
            Description = param.Description
        };
    }

    private static string MapJsonSchemaTypeName(string? jsonType, string? format)
        => MapJsonSchemaType(jsonType, format).Name;

    private static Type MapJsonSchemaType(string? jsonType, string? format)
    {
        // Build composite key: type+format (e.g., "integer+int64", "string+date-time")
        var sourceTypeName = jsonType?.ToLowerInvariant() ?? "string";
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

        // Return converter CLR type or fallback to string
        if (converter != JsonSchemaConverters.NotFound)
            return converter.TargetClrType;

        // Fallback to string for unknown types
        return typeof(string);
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
                httpClient.Timeout = TimeSpan.FromSeconds(30);
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
