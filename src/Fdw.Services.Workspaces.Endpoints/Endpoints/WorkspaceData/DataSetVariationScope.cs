using System.Collections.Generic;
using System.Text.Json;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The filter a view's chain puts on a query: every view's stored filter, read into the route's filter tree and joined with
/// And, parent first (FDW-1032).
/// </summary>
/// <remarks>
/// Which stored filters apply is <see cref="DataSetVariationChain"/>'s answer, carried on the data set the read service
/// resolved (<see cref="ReadableDataSet.ViewFilters"/>); this class only reads each into the filter type the route takes.
/// </remarks>
internal static class DataSetVariationScope
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The filter the stored filters make together.</summary>
    /// <param name="storedFilters">The stored filter texts, parent first.</param>
    /// <param name="viewName">The view's name, for a refusal.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The filter: null when there are none, the one itself when there is one, an And group when there are several. A text
    /// that is not a filter tree is <c>DataSetVariationInvalid</c>.
    /// </returns>
    internal static IGenericResult<QueryFilterNode?> Of(IReadOnlyList<string> storedFilters, string viewName, ILogger logger)
    {
        var nodes = new List<QueryFilterNode?>();
        foreach (var text in storedFilters)
        {
            var node = Read(text, viewName, logger);
            if (node.IsFailure) return node.ToNewResult<QueryFilterNode?>();
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
                ? DataSetVariationChain.Invalid<QueryFilterNode>(logger, viewName, "a stored filter is null, not a filter tree")
                : GenericResult<QueryFilterNode>.Success(node);
        }
        catch (JsonException ex)
        {
            return DataSetVariationChain.Invalid<QueryFilterNode>(logger, viewName, $"a stored filter is not a valid filter tree: {ex.Message}");
        }
    }
}
