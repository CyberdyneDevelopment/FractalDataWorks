using System.Collections.Generic;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads the stored filters of a view and its parents as the one filter tree a query is narrowed by (FDW-2391).
/// </summary>
/// <remarks>
/// Each stored filter is the JSON of a <see cref="QueryFilterNode"/>, the shape a data route takes in its body. They are ANDed in the order
/// given, the topmost parent first. A stored filter that cannot be read is refused by naming the view; it is never skipped, because a
/// skipped filter would return rows the view is meant to exclude.
/// </remarks>
public static class StoredViewFilters
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Reads stored filters as one filter tree.</summary>
    /// <param name="storedFilters">The stored filter texts, topmost parent first; empty for a data set.</param>
    /// <param name="viewName">The name asked for, for a refusal.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>No filter for none, the filter itself for one, an AND of them for several; or the refusal.</returns>
    public static IGenericResult<QueryFilterNode?> Of(IReadOnlyList<string> storedFilters, string viewName, ILogger logger)
    {
        var nodes = new List<QueryFilterNode?>();
        foreach (var text in storedFilters)
        {
            var node = Read(text, viewName, logger);
            if (node.IsFailure)
                return node.ToNewResult<QueryFilterNode?>();

            nodes.Add(node.Value);
        }

        return GenericResult<QueryFilterNode?>.Success(nodes.Count switch
        {
            0 => null,
            1 => nodes[0],
            _ => new QueryFilterNode { Logic = "And", Nodes = nodes },
        });
    }

    private static IGenericResult<QueryFilterNode> Read(string text, string viewName, ILogger logger)
    {
        try
        {
            var node = JsonSerializer.Deserialize<QueryFilterNode>(text, Json);
            return node is null
                ? GenericResult<QueryFilterNode>.Failure(DataSetEndpointLog.ViewFilterInvalid(logger, viewName, "a stored filter is null, not a filter tree"))
                : GenericResult<QueryFilterNode>.Success(node);
        }
        catch (JsonException ex)
        {
            return GenericResult<QueryFilterNode>.Failure(
                DataSetEndpointLog.ViewFilterInvalid(logger, viewName, $"a stored filter is not a valid filter tree: {ex.Message}"));
        }
    }
}
