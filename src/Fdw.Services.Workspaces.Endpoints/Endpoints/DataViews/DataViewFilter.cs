using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
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
    /// <param name="calculations">Evaluates the calculations the filter names; null for a host that has none.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The filter (null when there is none), or the refusal.</returns>
    /// <remarks>
    /// A calculation the filter names is evaluated here, so a name that does not resolve, an input without a value or a result of
    /// the wrong type for its field refuses the save, as it would refuse the query. The stored text is not changed: the view keeps
    /// the calculation's name and inputs and is evaluated again each time it is queried.
    /// </remarks>
    public static async Task<IGenericResult<IFilterExpression?>> Translate(
        string? filters,
        IReadOnlyList<DataSetFieldConfiguration> fields,
        IQueryRefusals refusals,
        IFilterCalculations? calculations,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filters)) return GenericResult<IFilterExpression?>.Success(null);

        var node = Read(filters, refusals);
        if (node.IsFailure) return node.ToNewResult<IFilterExpression?>();

        var translator = QueryTranslator.Create(
            fields.Select(f => new QueryableField(f.Name, f.Name, DataSetFieldTypes.Resolve(f.TypeName), f.IsCalculated)),
            "the subject data set",
            refusals);
        if (translator.IsFailure) return translator.ToNewResult<IFilterExpression?>();

        var resolved = await translator.Value!.ResolveCalculations(node.Value, calculations, cancellationToken).ConfigureAwait(false);
        return resolved.IsFailure
            ? resolved.ToNewResult<IFilterExpression?>()
            : translator.Value!.Filter(resolved.Value);
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
