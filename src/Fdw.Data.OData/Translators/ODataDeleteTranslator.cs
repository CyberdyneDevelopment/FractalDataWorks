using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.OData.Results;
using Fdw.Results;

namespace Fdw.Data.OData;

/// <summary>
/// Translates DeleteCommand to REST DELETE request.
/// </summary>
/// <remarks>
/// <para>
/// Builds HTTP DELETE requests for removing resources:
/// <list type="bullet">
/// <item>Method: DELETE</item>
/// <item>Path: Container name + ID (e.g., "/api/Customers/123")</item>
/// </list>
/// </para>
/// <para>
/// ALWAYS requires a resource ID from Filter expression for safety.
/// Does not support bulk deletes - each DELETE targets a specific resource.
/// </para>
/// </remarks>
[TypeOption(typeof(ODataCommandTranslators), "ODataDelete", RestrictToCurrentCompilation = true)]
public sealed class ODataDeleteTranslator : ODataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataDeleteTranslator"/> class.
    /// </summary>
    public ODataDeleteTranslator()
        : base("ODataDelete")
    {
    }

    /// <summary>
    /// Translates a DeleteCommand to a REST DELETE request.
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

            // Get filter from metadata (REQUIRED for safety)
            if (command.Metadata == null || !command.Metadata.TryGetValue("Filter", out var filterObj))
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("DeleteFilterRequired")));
            }

            var filter = filterObj as IFilterExpression;
            if (filter == null || filter.Root == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("DeleteFilterInvalid")));
            }

            // Extract resource ID from filter
            var resourceId = ExtractResourceId(filter, container);
            if (resourceId == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("DeleteResourceIdNotFound")));
            }

            // Build relative path with resource ID
            var basePath = container.Name.StartsWith('/')
                ? container.Name
                : $"/{container.Name}";
            var relativePath = $"{basePath}/{resourceId}";

            // Get HTTP DELETE request
            var request = new HttpRequestMessage(HttpMethod.Delete, relativePath);

            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Success(request));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Failure(
                    ODataResultCodes.ByName("DeleteTranslationFailed"),
                    ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Extracts the resource ID from filter expression by traversing hierarchical filter tree.
    /// </summary>
    private static object? ExtractResourceId(IFilterExpression filter, IStorageContainer container)
    {
        if (filter.Root == null)
        {
            return null;
        }

        // Why: IsPrimaryKey removed from IField — use GetPrimaryKeyFieldName() to locate the PK field.
        var pkFieldName = container.GetPrimaryKeyFieldName();

        // Recursively search for the ID condition
        return FindIdValue(filter.Root, pkFieldName);
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
