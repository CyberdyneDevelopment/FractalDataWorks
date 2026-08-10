using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Web.RestEndpoints.OpenApi;

/// <summary>
/// NSwag document processor that enriches the OpenAPI spec with per-dataset query documentation.
/// Adds dataset name enum constraints and per-dataset field tables to the query operation description.
/// </summary>
public sealed class DataSetQueryDocumentProcessor : IDocumentProcessor
{
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Sets the service provider for resolving IDataSetConfigurationProvider at document generation time.
    /// Must be called after app.Build() and DataGatewayTypes.Initialize().
    /// </summary>
    public void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void Process(DocumentProcessorContext context)
    {
        if (_serviceProvider is null)
            return;

        // Why: IDataSetConfigurationProvider (not IDataSetProvider) is used here because OpenAPI
        // document generation needs DataSetConfiguration records — specifically Fields, KeyFields,
        // and source metadata — not the live IDataSet runtime graph.
        var dataSetProvider = _serviceProvider.GetService<IDataSetConfigurationProvider>();
        if (dataSetProvider is null)
            return;

#pragma warning disable VSTHRD002 // NSwag IDocumentProcessor.Process() is sync — no async path available
        var allDataSetsResult = dataSetProvider.Get().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (!allDataSetsResult.IsSuccess || allDataSetsResult.Value is null || allDataSetsResult.Value.Count == 0)
            return;

        var allDataSets = allDataSetsResult.Value.ToList();
        if (allDataSets.Count == 0)
            return;

        var dataSetNames = allDataSets
            .Select(ds => ds.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Find the query operation
        var queryPath = FindQueryOperation(context.Document);
        if (queryPath is null)
            return;

        var (pathItem, operation) = queryPath.Value;

        // Enrich DataSetName parameter with enum constraint
        EnrichDataSetNameParameter(operation, dataSetNames);

        // Build per-dataset field documentation in the description
        EnrichDescription(operation, allDataSets);

        // Clone the operation per dataset for Scalar sidebar entries
        ClonePerDataSet(context.Document, pathItem, operation, allDataSets);
    }

    private static (OpenApiPathItem PathItem, OpenApiOperation Operation)? FindQueryOperation(
        OpenApiDocument document)
    {
        foreach (var (path, pathItem) in document.Paths)
        {
            if (!path.Contains("/datasets/", StringComparison.OrdinalIgnoreCase) ||
                !path.Contains("/query", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pathItem.TryGetValue(OpenApiOperationMethod.Get, out var operation))
            {
                return (pathItem, operation);
            }
        }

        return null;
    }

    private static void EnrichDataSetNameParameter(
        OpenApiOperation operation,
        IReadOnlyList<string> dataSetNames)
    {
        var param = operation.Parameters
            .FirstOrDefault(p => string.Equals(p.Name, "DataSetName", StringComparison.OrdinalIgnoreCase));

        if (param is null)
            return;

        param.Schema ??= new JsonSchema();
        param.Schema.Type = JsonObjectType.String;
        foreach (var name in dataSetNames)
        {
            param.Schema.Enumeration.Add(name);
        }

        param.Description = "The name of the DataSet to query. Available datasets: " +
                            string.Join(", ", dataSetNames);
    }

    private static void EnrichDescription(
        OpenApiOperation operation,
        IReadOnlyList<DataSetConfiguration> allDataSets)
    {
        var description = operation.Description ?? string.Empty;
        description += "\n\n---\n\n## Available DataSets\n\n";

        foreach (var dataSet in allDataSets.OrderBy(ds => ds.Name, StringComparer.OrdinalIgnoreCase))
        {
            description += $"### {dataSet.Name}\n\n";

            if (!string.IsNullOrEmpty(dataSet.Description))
            {
                description += $"{dataSet.Description}\n\n";
            }

            if (dataSet.Fields.Count > 0)
            {
                description += "| Field | Type | Key | Indexed | Role |\n";
                description += "|-------|------|-----|---------|------|\n";

                foreach (var field in dataSet.Fields.OrderBy(f => f.Ordinal))
                {
                    var key = field.IsKey ? "Yes" : "";
                    var indexed = field.IsIndexed ? "Yes" : "";
                    var role = field.Role ?? "";
                    description += $"| `{field.Name}` | {field.TypeName} | {key} | {indexed} | {role} |\n";
                }

                description += "\n";
            }

            // Document key fields
            if (dataSet.KeyFields.Count > 0)
            {
                description += $"**Key fields:** {string.Join(", ", dataSet.KeyFields.Select(k => $"`{k.KeyName}` ({k.KeyType})"))}\n\n";
            }
        }

        operation.Description = description;
    }

    private static void ClonePerDataSet(
        OpenApiDocument document,
        OpenApiPathItem originalPathItem,
        OpenApiOperation originalOperation,
        IReadOnlyList<DataSetConfiguration> allDataSets)
    {
        // Find and remove the generic path
        string? genericPath = null;
        foreach (var (path, item) in document.Paths)
        {
            if (ReferenceEquals(item, originalPathItem))
            {
                genericPath = path;
                break;
            }
        }

        if (genericPath is null)
            return;

        // =====================================================================================
        // Multi-parameter cascading clone pattern
        // =====================================================================================
        // This method currently clones on a single parameter ({DataSetName}).
        // To support multiple dependent URL parameters (e.g., /api/v1/{Organization}/{DataSetName}/query),
        // nest the loops — each outer loop resolves the parent, each inner loop gets its children:
        //
        //   var organizations = GetAllOrganizations();
        //   foreach (var org in organizations)
        //   {
        //       var orgDataSets = GetDataSetsForOrganization(org);
        //       foreach (var dataSet in orgDataSets)
        //       {
        //           var path = genericPath
        //               .Replace("{Organization}", org.Name, StringComparison.OrdinalIgnoreCase)
        //               .Replace("{DataSetName}", dataSet.Name, StringComparison.OrdinalIgnoreCase);
        //
        //           // Build clonedOperation the same way as below, but:
        //           //   - Skip both {Organization} and {DataSetName} from original params
        //           //   - Use Tags($"DataSets - {org.Name}") to group in Scalar sidebar
        //           //   - OperationId = $"QueryDataSet_{org.Name}_{dataSet.Name}"
        //       }
        //   }
        //
        // Each combination appears as its own Scalar entry with only valid fields for that pair.
        // For 3+ parameters, add another nesting level following the same pattern.
        // =====================================================================================

        // Add per-dataset paths
        foreach (var dataSet in allDataSets.OrderBy(ds => ds.Name, StringComparer.OrdinalIgnoreCase))
        {
            var dataSetPath = genericPath.Replace(
                "{DataSetName}", dataSet.Name, StringComparison.OrdinalIgnoreCase);

            var clonedOperation = new OpenApiOperation
            {
                Summary = $"Query {dataSet.Name}",
                Description = BuildDataSetDescription(dataSet),
                OperationId = $"QueryDataSet_{dataSet.Name}",
                IsDeprecated = originalOperation.IsDeprecated,
            };

            // Copy tags
            foreach (var tag in originalOperation.Tags)
            {
                clonedOperation.Tags.Add(tag);
            }

            // Copy skip/take parameters (not DataSetName since it's now in the path)
            foreach (var param in originalOperation.Parameters)
            {
                if (string.Equals(param.Name, "DataSetName", StringComparison.OrdinalIgnoreCase))
                    continue;

                clonedOperation.Parameters.Add(param);
            }

            // Add field-specific query parameters for this dataset
            foreach (var field in dataSet.Fields.OrderBy(f => f.Ordinal))
            {
                var fieldParam = new OpenApiParameter
                {
                    Name = field.Name,
                    Kind = OpenApiParameterKind.Query,
                    IsRequired = false,
                    Description = BuildFieldParameterDescription(field),
                    Schema = MapFieldTypeToSchema(field.TypeName)
                };

                clonedOperation.Parameters.Add(fieldParam);
            }

            // Copy responses
            foreach (var (statusCode, response) in originalOperation.Responses)
            {
                clonedOperation.Responses[statusCode] = response;
            }

            var pathItem = new OpenApiPathItem();
            pathItem[OpenApiOperationMethod.Get] = clonedOperation;
            document.Paths[dataSetPath] = pathItem;
        }

        // Remove the generic path since we've cloned per-dataset
        document.Paths.Remove(genericPath);
    }

    private static string BuildDataSetDescription(DataSetConfiguration dataSet)
    {
        var description = $"Query the **{dataSet.Name}** dataset.";

        if (!string.IsNullOrEmpty(dataSet.Description))
        {
            description += $" {dataSet.Description}";
        }

        if (dataSet.Fields.Count > 0)
        {
            description += "\n\n| Field | Type | Key | Indexed | Role |\n";
            description += "|-------|------|-----|---------|------|\n";

            foreach (var field in dataSet.Fields.OrderBy(f => f.Ordinal))
            {
                var key = field.IsKey ? "Yes" : "";
                var indexed = field.IsIndexed ? "Yes" : "";
                var role = field.Role ?? "";
                description += $"| `{field.Name}` | {field.TypeName} | {key} | {indexed} | {role} |\n";
            }
        }

        return description;
    }

    private static string BuildFieldParameterDescription(DataFieldConfiguration field)
    {
        var parts = new List<string> { $"Type: {field.TypeName}" };
        if (field.IsKey) parts.Add("Primary Key");
        if (field.IsIndexed) parts.Add("Indexed");
        if (!string.IsNullOrEmpty(field.Role)) parts.Add($"Role: {field.Role}");
        if (field.MaxLength.HasValue) parts.Add($"Max length: {field.MaxLength}");
        return string.Join(" | ", parts);
    }

    private static JsonSchema MapFieldTypeToSchema(string typeName)
    {
        var schema = new JsonSchema();

        if (string.Equals(typeName, "Int32", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "Int64", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase))
        {
            schema.Type = JsonObjectType.Integer;
        }
        else if (string.Equals(typeName, "Boolean", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(typeName, "bool", StringComparison.OrdinalIgnoreCase))
        {
            schema.Type = JsonObjectType.Boolean;
        }
        else if (string.Equals(typeName, "Decimal", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(typeName, "Single", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(typeName, "float", StringComparison.OrdinalIgnoreCase))
        {
            schema.Type = JsonObjectType.Number;
        }
        else
        {
            schema.Type = JsonObjectType.String;
        }

        return schema;
    }
}
