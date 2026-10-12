using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Reads one data view, whole: <c>POST /data-views/get</c>, its name in the body (FDW-978).</summary>
/// <remarks>
/// The list answers <see cref="DataViewSummaryResponse"/>, which leaves out the view's filters; the read of one
/// view carries them, and the whole spec: encoding, chart type, the data set it reads and the view it is a view of. It is a
/// POST because a GET is a plain paged list and nothing else.
/// </remarks>
public abstract class GetDataViewEndpointBase : CrudGetEndpointBase<DataViewNameRequest, DataViewResponse>
{
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <inheritdoc />
    protected GetDataViewEndpointBase(ILogger<GetDataViewEndpointBase> logger, IDataSetVariationConfigurationProvider variations) : base(logger)
    {
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "data-views";

    /// <summary>Gets whether the data view is read by POST with its name in the body: it is.</summary>
    protected override bool ReadByBody => true;

    /// <inheritdoc />
    protected override string GetResourceIdentifier(DataViewNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataViewResponse?>> FindByIdentifier(DataViewNameRequest request, CancellationToken ct)
    {
        var found = await DataViewStore.Find(_variations, request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found.ToNewResult<DataViewResponse?>();
        if (found.Value is not { } view) return GenericResult<DataViewResponse?>.Success(null);

        var parentName = await DataViewStore.ParentName(_variations, view, Logger, ct).ConfigureAwait(false);
        return parentName.IsFailure
            ? parentName.ToNewResult<DataViewResponse?>()
            : GenericResult<DataViewResponse?>.Success(DataViewResponseMapper.ToResponse(view, parentName.Value));
    }
}
