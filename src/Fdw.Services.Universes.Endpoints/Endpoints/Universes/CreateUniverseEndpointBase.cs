using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Universes.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Creates a universe.</summary>
public abstract class CreateUniverseEndpointBase : CrudCreateEndpointBase<CreateUniverseRequest, UniverseDetailResponse>
{
    private readonly IUniverseConfigurationProvider _provider;

    /// <inheritdoc />
    private readonly IAuthenticationContextAccessor _authContext;

    /// <inheritdoc />
    protected CreateUniverseEndpointBase(
        ILogger<CreateUniverseEndpointBase> logger,
        IUniverseConfigurationProvider provider,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _provider = provider;
        _authContext = authContext;
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
        // Why this refuses rather than defaulting: OwnerUserId is a Guid and NOT NULL, so an
        // unresolved caller lands on Guid.Empty, stores cleanly, and reads back as an owner nobody
        // can resolve. Ownership is also what universes:write is to be scoped by (FDW-725), so a
        // universe created with no real owner is one nobody can edit once that lands -- including
        // whoever created it. There are two ways to have no owner and neither is defaultable: no
        // authentication context at all, and a UserId that is not a Guid.
        if (_authContext.Current is not { } caller)
        {
            return GenericResult<UniverseDetailResponse>.Failure(
                UniversesResultCodes.ByName("UniverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "no authentication context"));
        }

        if (!Guid.TryParse(caller.UserId, out var ownerUserId))
        {
            return GenericResult<UniverseDetailResponse>.Failure(
                UniversesResultCodes.ByName("UniverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "UserId is not a Guid"));
        }

        var config = new UniverseConfiguration
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
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
