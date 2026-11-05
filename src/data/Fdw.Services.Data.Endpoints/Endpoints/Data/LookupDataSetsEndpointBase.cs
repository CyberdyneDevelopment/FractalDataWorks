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
/// Base endpoint for looking up data sets by name (FDW-1194), for pickers and name-taken checks. A variation is a data set of kind
/// Variation (FDW-3638), so it is offered with the rest, and <c>?kind=</c> narrows the lookup to the kinds named.
/// </summary>
/// <remarks>
/// One query on the domain table: no data set's implementation, fields or sources are read. Without <c>?kind=</c> every data set is
/// offered, whatever its kind. <c>?kind=Simple,Variation</c> offers those kinds, in one name order; each item names its kind in
/// <c>implementation</c>. A kind that <see cref="DataSetTypes"/> does not hold is refused.
/// </remarks>
public abstract class LookupDataSetsEndpointBase : LookupEndpointBase<DomainConfiguration>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="LookupDataSetsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads the page of data set rows.</param>
    protected LookupDataSetsEndpointBase(ILogger<LookupDataSetsEndpointBase> logger, DataSetConfigurationProvider dataSetProvider)
        : base(logger)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Read(DomainQuery query, CancellationToken ct)
    {
        var kinds = RequestedDataSetKinds.Of(HttpContext.Request, Logger);
        if (kinds.IsFailure)
            return kinds.ToNewResult<ConfigurationPage<DomainConfiguration>>();

        return await _dataSetProvider.Get(kinds.Value!.Count == 0 ? query : RequestedDataSetKinds.Narrow(query, kinds.Value), ct).ConfigureAwait(false);
    }
}
