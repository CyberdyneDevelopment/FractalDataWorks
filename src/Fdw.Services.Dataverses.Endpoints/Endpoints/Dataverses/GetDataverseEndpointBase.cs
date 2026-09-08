using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Reads one dataverse with its members, resources and relationships.</summary>
public abstract class GetDataverseEndpointBase : CrudGetEndpointBase<DataverseNameRequest, DataverseDetailResponse>
{
    private readonly IDataverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetDataverseEndpointBase(ILogger<GetDataverseEndpointBase> logger, IDataverseConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(DataverseNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseDetailResponse?>> FindByIdentifier(
        DataverseNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<DataverseDetailResponse?>()
            : GenericResult<DataverseDetailResponse?>.Success(
                result.Value is null ? null : DataverseResponseMapper.ToDetail(result.Value));
    }
}
