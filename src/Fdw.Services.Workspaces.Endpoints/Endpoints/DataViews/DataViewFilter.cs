using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Checks a data view's stored filter the way the data route will check it when the chart is opened: the
/// same <see cref="QueryTranslator"/>, against the subject data set's current fields (FDW-1023).
/// </summary>
/// <remarks>
/// The stored text is a filter tree in the body shape of POST /data/{dataset} (<see cref="QueryFilterNode"/>).
/// A view with no filter is valid: blank text is no filter, not a refusal.
/// </remarks>
internal static class DataViewFilter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Translates a stored filter.</summary>
    /// <param name="filters">The stored filter text, or null.</param>
    /// <param name="fields">The subject data set's current fields.</param>
    /// <param name="refusals">Builds the refusal a filter that does not resolve is answered with.</param>
    /// <returns>The filter (null when there is none), or the refusal.</returns>
    public static IGenericResult<IFilterExpression?> Translate(
        string? filters, IReadOnlyList<DataSetFieldConfiguration> fields, IQueryRefusals refusals)
    {
        if (string.IsNullOrWhiteSpace(filters)) return GenericResult<IFilterExpression?>.Success(null);

        var node = Read(filters, refusals);
        if (node.IsFailure) return node.ToNewResult<IFilterExpression?>();

        var translator = QueryTranslator.Create(
            fields.Select(f => new QueryableField(f.Name, f.Name, DataSetFieldTypes.Resolve(f.TypeName), f.IsCalculated)),
            "the subject data set",
            refusals);
        return translator.IsFailure
            ? translator.ToNewResult<IFilterExpression?>()
            : translator.Value!.Filter(node.Value);
    }

    private static IGenericResult<QueryFilterNode> Read(string filters, IQueryRefusals refusals)
    {
        try
        {
            var node = JsonSerializer.Deserialize<QueryFilterNode>(filters, Options);
            return node is null
                ? refusals.Invalid<QueryFilterNode>("Filters must be a filter tree, not null")
                : GenericResult<QueryFilterNode>.Success(node);
        }
        catch (JsonException ex)
        {
            return refusals.Invalid<QueryFilterNode>($"Filters is not a valid filter tree: {ex.Message}");
        }
    }
}
