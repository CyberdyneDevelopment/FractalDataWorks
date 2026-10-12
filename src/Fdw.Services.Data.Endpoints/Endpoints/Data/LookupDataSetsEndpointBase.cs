using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for looking up data sets by name (FDW-1194), for pickers and name-taken checks.
/// </summary>
/// <remarks>
/// One query on the data sets domain table: no data set's implementation, fields or sources are read. It offers the rows
/// the data set list offers, which is every row.
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
    protected override Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Read(DomainQuery query, CancellationToken ct)
        => _dataSetProvider.Get(query, ct);
}
