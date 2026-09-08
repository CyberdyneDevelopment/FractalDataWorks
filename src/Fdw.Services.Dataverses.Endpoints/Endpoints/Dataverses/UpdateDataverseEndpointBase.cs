using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Modifies a dataverse.</summary>
/// <remarks>
/// A null field on the request means "leave it alone", which is what PATCH means. The existing
/// configuration is re-read and only the supplied fields are moved, so an omitted Status is never
/// mistaken for a request to blank it.
/// </remarks>
public abstract class UpdateDataverseEndpointBase : CrudUpdateEndpointBase<UpdateDataverseRequest, DataverseDetailResponse>
{
    private readonly IDataverseConfigurationProvider _provider;
    private readonly IDataverseAccessPolicy _access;

    /// <inheritdoc />
    protected UpdateDataverseEndpointBase(
        ILogger<UpdateDataverseEndpointBase> logger,
        IDataverseConfigurationProvider provider,
        IDataverseAccessPolicy access) : base(logger)
    {
        _provider = provider;
        _access = access;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateDataverseRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseDetailResponse?>> FindForUpdate(
        UpdateDataverseRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<DataverseDetailResponse?>()
            : GenericResult<DataverseDetailResponse?>.Success(
                result.Value is null ? null : DataverseResponseMapper.ToDetail(result.Value));
    }

    /// <summary>
    /// Rejects a supplied Status, Visibility or JoinPolicy that is not a registered option.
    /// </summary>
    /// <remarks>
    /// Only what was supplied is checked. A null field means "leave it alone", so validating it
    /// would reject every PATCH that does not restate all three.
    /// </remarks>
    /// <param name="request">The update request.</param>
    protected virtual IGenericResult ValidateLifecycle(UpdateDataverseRequest request)
    {
        var status = DataverseLifecycleValidator.ValidateStatus(request.Name, request.Status, Logger);
        if (status.IsFailure) return status;

        var visibility = DataverseLifecycleValidator.ValidateVisibility(request.Name, request.Visibility, Logger);
        return visibility.IsFailure
            ? visibility
            : DataverseLifecycleValidator.ValidateJoinPolicy(request.Name, request.JoinPolicy, Logger);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseDetailResponse>> Update(
        UpdateDataverseRequest request, DataverseDetailResponse existing, CancellationToken ct)
    {
        var lifecycle = ValidateLifecycle(request);
        if (lifecycle.IsFailure) return lifecycle.ToNewResult<DataverseDetailResponse>();

        var current = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (current.IsFailure) return current.ToNewResult<DataverseDetailResponse>();

        if (current.Value is null)
        {
            return GenericResult<DataverseDetailResponse>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var config = current.Value;

        // After the load, because the decision is about THIS dataverse: who owns it and who is in
        // it are on the row. dataverses:write got the caller this far and says nothing about which
        // dataverse they may change.
        var permitted = await _access.MayWrite(config, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<DataverseDetailResponse>();

        config.DisplayName = request.DisplayName ?? config.DisplayName;
        config.Description = request.Description ?? config.Description;
        config.Purpose = request.Purpose ?? config.Purpose;
        config.Status = request.Status ?? config.Status;
        config.Visibility = request.Visibility ?? config.Visibility;
        config.JoinPolicy = request.JoinPolicy ?? config.JoinPolicy;
        config.StandInSeed = request.StandInSeed ?? config.StandInSeed;

        var saved = await _provider.Save(config, ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<DataverseDetailResponse>()
            : GenericResult<DataverseDetailResponse>.Success(DataverseResponseMapper.ToDetail(config));
    }
}
