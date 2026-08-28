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

    /// <inheritdoc />
    protected override async Task<IGenericResult<UniverseDetailResponse>> Create(
        CreateUniverseRequest request, CancellationToken ct)
    {
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
