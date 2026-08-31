using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Creates a universe.</summary>
public abstract class CreateUniverseEndpointBase : CrudCreateEndpointBase<CreateUniverseRequest, UniverseDetailResponse>
{
    private readonly IUniverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateUniverseEndpointBase(IUniverseConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "universes";

    /// <inheritdoc />
    protected override string GetResourceName(CreateUniverseRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateUniverseRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<bool>()
            : GenericResult<bool>.Success(result.Value is not null);
    }

    /// <summary>
    /// Rejects a Status, Visibility or JoinPolicy that is not a registered option.
    /// </summary>
    /// <remarks>
    /// All three are required on create, and an empty string is not a registered option, so the
    /// same check that rejects a typo also rejects an omission. Nothing is substituted: a project
    /// silently created Private and Closed, or silently Open, is a decision the caller did not make.
    /// </remarks>
    /// <param name="request">The create request.</param>
    protected virtual IGenericResult ValidateLifecycle(CreateUniverseRequest request)
    {
        var status = UniverseLifecycleValidator.ValidateStatus(request.Name, request.Status, Logger);
        if (status.IsFailure) return status;

        var visibility = UniverseLifecycleValidator.ValidateVisibility(request.Name, request.Visibility, Logger);
        return visibility.IsFailure
            ? visibility
            : UniverseLifecycleValidator.ValidateJoinPolicy(request.Name, request.JoinPolicy, Logger);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<UniverseDetailResponse>> Create(
        CreateUniverseRequest request, CancellationToken ct)
    {
        var lifecycle = ValidateLifecycle(request);
        if (lifecycle.IsFailure) return lifecycle.ToNewResult<UniverseDetailResponse>();

        // Why Guid.CreateVersion7: the database has no DEFAULT on Id and never mints one. A
        // time-ordered id also keeps insert order and sort order the same thing.
        var config = new UniverseConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Purpose = request.Purpose,
            Status = request.Status,
            Visibility = request.Visibility,
            JoinPolicy = request.JoinPolicy,
            StandInSeed = request.StandInSeed,
        };

        var saved = await _provider.Save(config, ct).ConfigureAwait(false);

        // Map what we persisted, not the result's value: Save succeeded on this object, so it is
        // the authoritative shape and needs no null dance.
        return saved.IsFailure
            ? saved.ToNewResult<UniverseDetailResponse>()
            : GenericResult<UniverseDetailResponse>.Success(UniverseResponseMapper.ToDetail(config));
    }
}
