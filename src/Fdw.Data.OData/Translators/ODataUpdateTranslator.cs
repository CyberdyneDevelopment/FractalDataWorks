using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.OData.Results;
using Fdw.Results;

namespace Fdw.Data.OData;

/// <summary>
/// Translates UpdateCommand to REST PUT/PATCH request with JSON body.
/// </summary>
/// <remarks>
/// <para>
/// Builds HTTP PUT requests for updating existing resources:
/// <list type="bullet">
/// <item>Method: PUT (or PATCH for partial updates)</item>
/// <item>Path: Container name + ID (e.g., "/api/Customers/123")</item>
/// <item>Body: JSON serialization of entity data</item>
/// <item>Content-Type: application/json</item>
/// </list>
/// </para>
/// <para>
/// Extracts resource ID from Filter expression or entity data primary key field.
/// </para>
/// </remarks>
[TypeOption(typeof(ODataCommandTranslators), "ODataUpdate", RestrictToCurrentCompilation = true)]
public sealed class ODataUpdateTranslator : ODataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataUpdateTranslator"/> class.
    /// </summary>
    public ODataUpdateTranslator()
        : base("ODataUpdate")
    {
    }

    /// <summary>
    /// Translates an UpdateCommand to a REST PUT request.
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the HttpRequestMessage.</returns>
    public override Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("ContainerNull")));
            }

            // Get entity data from metadata
            if (command.Metadata == null || !command.Metadata.TryGetValue("Data", out var dataObj) || dataObj == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("UpdateDataRequired")));
            }

            // Extract resource ID from filter or primary key
            var resourceId = ExtractResourceId(command, container, dataObj);
            if (resourceId == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("UpdateResourceIdNotFound")));
            }

            // Build relative path with resource ID
            var basePath = container.Name.StartsWith('/')
                ? container.Name
                : $"/{container.Name}";
            var relativePath = $"{basePath}/{resourceId}";

            // Serialize data to JSON
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(dataObj);

            // Get HTTP PUT request with JSON body
            var request = new HttpRequestMessage(HttpMethod.Put, relativePath)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Success(request));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Failure(
                    ODataResultCodes.ByName("UpdateTranslationFailed"),
                    ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Extracts the resource ID from filter expression or entity primary key.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Resource ID extraction with multiple fallback strategies (filter traversal, schema PK, reflection, common names)
    private static object? ExtractResourceId(IDataCommand command, IStorageContainer container, object data)
    {
        // Try to get ID from filter first
        if (command.Metadata?.TryGetValue("Filter", out var filterObj) == true &&
            filterObj is IFilterExpression filter &&
            filter.Root != null)
        {
            // Why: IsPrimaryKey removed from IField — use GetPrimaryKeyFieldName() to locate the PK field.
            var pkFieldName = container.GetPrimaryKeyFieldName();
            var idValue = FindIdValue(filter.Root, pkFieldName);
            if (idValue != null)
            {
                return idValue;
            }
        }

        // Fall back to extracting from data object
        var dataType = data.GetType();
        var pkName = container.GetPrimaryKeyFieldName();
        var pkProperty = pkName != null ? container.Schema?.Fields?.FirstOrDefault(f => string.Equals(f.Name, pkName, StringComparison.OrdinalIgnoreCase)) : null;
        if (pkProperty != null)
        {
            var property = dataType.GetProperty(pkProperty.Name);
            if (property != null)
            {
                return property.GetValue(data);
            }
        }

        // Try common ID property names
        var idProperty = dataType.GetProperty("Id") ?? dataType.GetProperty("ID");
        return idProperty?.GetValue(data);
    }

    /// <summary>
    /// Recursively searches for ID value in filter node tree.
    /// </summary>
    private static object? FindIdValue(IFilterNode node, string? pkFieldName)
    {
        if (node is FilterCondition condition)
        {
            // Check if this condition matches primary key or common ID field names
            if (pkFieldName != null && string.Equals(condition.PropertyName, pkFieldName, StringComparison.OrdinalIgnoreCase))
            {
                return condition.Value;
            }

            if (string.Equals(condition.PropertyName, "Id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(condition.PropertyName, "ID", StringComparison.OrdinalIgnoreCase))
            {
                return condition.Value;
            }

            return null;
        }

        if (node is FilterGroup group)
        {
            // Search all child nodes
            foreach (var childNode in group.Nodes)
            {
                var value = FindIdValue(childNode, pkFieldName);
                if (value != null)
                {
                    return value;
                }
            }
        }

        return null;
    }
}
