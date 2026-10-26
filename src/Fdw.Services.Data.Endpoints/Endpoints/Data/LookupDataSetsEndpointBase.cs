using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for looking up data sets by name (FDW-1194), for pickers and name-taken checks, and with <c>?kind=</c> the
/// variations of data sets, such as saved views, beside them (FDW-2392).
/// </summary>
/// <remarks>
/// One query on the domain table of each kind asked for: no data set's implementation, fields or sources are read. Without
/// <c>?kind=</c> it offers the data sets, which is every row of that table, so a lookup that must not offer a view does not.
/// <c>?kind=DataSet,DataView</c> offers both, in one name order; each item names its kind in <c>implementation</c>. A kind this host
/// does not know is refused.
/// </remarks>
public abstract class LookupDataSetsEndpointBase : LookupEndpointBase<DomainConfiguration>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <summary>Initializes a new instance of the <see cref="LookupDataSetsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads the page of data set rows.</param>
    /// <param name="variations">Reads the page of variation rows, and says which kinds of variation this host registers.</param>
    protected LookupDataSetsEndpointBase(
        ILogger<LookupDataSetsEndpointBase> logger, DataSetConfigurationProvider dataSetProvider, IDataSetVariationConfigurationProvider variations)
        : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Read(DomainQuery query, CancellationToken ct)
    {
        var kinds = Requested();
        if (kinds.IsFailure)
            return kinds.ToNewResult<ConfigurationPage<DomainConfiguration>>();

        if (kinds.Value!.Count == 0)
            return await _dataSetProvider.Get(query, ct).ConfigureAwait(false);

        var sources = new List<MergedDomainPages.Source>();
        if (kinds.Value.Contains(ResolvedDataSet.DataSetKind, StringComparer.OrdinalIgnoreCase))
            sources.Add(async (wide, token) => await Collect(() => _dataSetProvider.Get(wide, token), token).ConfigureAwait(false));

        var variationKinds = kinds.Value.Where(kind => !string.Equals(kind, ResolvedDataSet.DataSetKind, StringComparison.OrdinalIgnoreCase)).ToList();
        if (variationKinds.Count > 0)
            sources.Add(async (wide, token) => await Collect(() => _variations.Get(OfKinds(wide, variationKinds), token), token).ConfigureAwait(false));

        return await MergedDomainPages.Read(sources, query, ct).ConfigureAwait(false);
    }

    // Reads ?kind=A,B (or several kind= parameters) as the kinds asked for: none when it is absent, else each once, and each one this host knows.
    private IGenericResult<IReadOnlyList<string>> Requested()
    {
        var asked = HttpContext.Request.Query["kind"]
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var kind in asked)
        {
            if (!string.Equals(kind, ResolvedDataSet.DataSetKind, StringComparison.OrdinalIgnoreCase) && _variations.Implementation(kind).IsFailure)
                return GenericResult<IReadOnlyList<string>>.Failure(DataSetEndpointLog.LookupKindUnknown(Logger, kind));
        }

        return GenericResult<IReadOnlyList<string>>.Success(asked);
    }

    // The caller's query, kept to the rows of the kinds asked for: the kind of a row is the implementation its domain row names.
    private static DomainQuery OfKinds(DomainQuery query, IReadOnlyList<string> kinds)
    {
        var kind = new FilterCondition
        {
            PropertyName = nameof(DomainConfiguration.Implementation),
            Operator = FilterOperators.ByName("In"),
            Value = kinds.Cast<object?>().ToList(),
        };

        var root = query.Filter?.Root is { } caller
            ? (IFilterNode)new FilterGroup { Operator = LogicalOperator.And, Nodes = [caller, kind] }
            : kind;
        return new DomainQuery(new FilterExpression { Root = root }, query.Ordering, query.Skip, query.Take, query.IncludeTotal);
    }

    private static async Task<IGenericResult<MergedDomainPages.Rows>> Collect<T>(
        Func<Task<IGenericResult<ConfigurationPage<T>>>> read, CancellationToken token)
        where T : DomainConfiguration
    {
        var page = await read().ConfigureAwait(false);
        return page.IsFailure
            ? page.ToNewResult<MergedDomainPages.Rows>()
            : await MergedDomainPages.Collect(page.Value!, token).ConfigureAwait(false);
    }
}
