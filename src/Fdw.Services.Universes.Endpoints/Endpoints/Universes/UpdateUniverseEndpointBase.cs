using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Universes.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Modifies a universe.</summary>
/// <remarks>
/// A null field on the request means "leave it alone", which is what PATCH means. The existing
/// configuration is re-read and only the supplied fields are moved, so an omitted Status is never
/// mistaken for a request to blank it.
/// </remarks>
public abstract class UpdateUniverseEndpointBase : CrudUpdateEndpointBase<UpdateUniverseRequest, UniverseDetailResponse>
{
    private readonly IUniverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected UpdateUniverseEndpointBase(IUniverseConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "universes";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateUniverseRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<UniverseDetailResponse?>> FindForUpdate(
        UpdateUniverseRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<UniverseDetailResponse?>()
            : GenericResult<UniverseDetailResponse?>.Success(
                result.Value is null ? null : UniverseResponseMapper.ToDetail(result.Value));
    }

    /// <summary>
    /// Rejects a supplied Status, Visibility or JoinPolicy that is not a registered option.
    /// </summary>
    /// <remarks>
    /// Only what was supplied is checked. A null field means "leave it alone", so validating it
    /// would reject every PATCH that does not restate all three.
    /// </remarks>
    /// <param name="request">The update request.</param>
    protected virtual IGenericResult ValidateLifecycle(UpdateUniverseRequest request)
    {
        var status = UniverseLifecycleValidator.ValidateStatus(request.Name, request.Status, Logger);
        if (status.IsFailure) return status;

        var visibility = UniverseLifecycleValidator.ValidateVisibility(request.Name, request.Visibility, Logger);
        return visibility.IsFailure
            ? visibility
            : UniverseLifecycleValidator.ValidateJoinPolicy(request.Name, request.JoinPolicy, Logger);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<UniverseDetailResponse>> Update(
        UpdateUniverseRequest request, UniverseDetailResponse existing, CancellationToken ct)
    {
        var lifecycle = ValidateLifecycle(request);
        if (lifecycle.IsFailure) return lifecycle.ToNewResult<UniverseDetailResponse>();

        var current = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (current.IsFailure) return current.ToNewResult<UniverseDetailResponse>();

        if (current.Value is null)
        {
            return GenericResult<UniverseDetailResponse>.Failure(
                UniversesResultCodes.ByName("UniverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var config = current.Value;
        config.DisplayName = request.DisplayName ?? config.DisplayName;
        config.Description = request.Description ?? config.Description;
        config.Purpose = request.Purpose ?? config.Purpose;
        config.Status = request.Status ?? config.Status;
        config.Visibility = request.Visibility ?? config.Visibility;
        config.JoinPolicy = request.JoinPolicy ?? config.JoinPolicy;
        config.StandInSeed = request.StandInSeed ?? config.StandInSeed;

        var saved = await _provider.Save(config, ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<UniverseDetailResponse>()
            : GenericResult<UniverseDetailResponse>.Success(UniverseResponseMapper.ToDetail(config));
    }
}
