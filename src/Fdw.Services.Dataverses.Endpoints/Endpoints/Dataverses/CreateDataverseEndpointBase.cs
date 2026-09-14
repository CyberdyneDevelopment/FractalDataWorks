using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Creates a dataverse.</summary>
public abstract class CreateDataverseEndpointBase : CrudCreateEndpointBase<CreateDataverseRequest, DataverseDetailResponse>
{
    private readonly IDataverseConfigurationProvider _provider;

    /// <inheritdoc />
    private readonly IAuthenticationContextAccessor _authContext;

    /// <inheritdoc />
    protected CreateDataverseEndpointBase(
        ILogger<CreateDataverseEndpointBase> logger,
        IDataverseConfigurationProvider provider,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _provider = provider;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string GetResourceName(CreateDataverseRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateDataverseRequest request, CancellationToken ct)
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
    protected virtual IGenericResult ValidateLifecycle(CreateDataverseRequest request)
    {
        var status = DataverseLifecycleValidator.ValidateStatus(request.Name, request.Status, Logger);
        if (status.IsFailure) return status;

        var visibility = DataverseLifecycleValidator.ValidateVisibility(request.Name, request.Visibility, Logger);
        return visibility.IsFailure
            ? visibility
            : DataverseLifecycleValidator.ValidateJoinPolicy(request.Name, request.JoinPolicy, Logger);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseDetailResponse>> Create(
        CreateDataverseRequest request, CancellationToken ct)
    {
        var lifecycle = ValidateLifecycle(request);
        if (lifecycle.IsFailure) return lifecycle.ToNewResult<DataverseDetailResponse>();

        // Why Guid.CreateVersion7: the database has no DEFAULT on Id and never mints one. A
        // time-ordered id also keeps insert order and sort order the same thing.
        // Why this refuses rather than defaulting: OwnerUserId is a Guid and NOT NULL, so an
        // unresolved caller lands on Guid.Empty, stores cleanly, and reads back as an owner nobody
        // can resolve. Ownership is also what dataverses:write is to be scoped by (FDW-725), so a
        // dataverse created with no real owner is one nobody can edit once that lands -- including
        // whoever created it. There are two ways to have no owner and neither is defaultable: no
        // authentication context at all, and a UserId that is not a Guid.
        if (_authContext.Current is not { } caller)
        {
            return GenericResult<DataverseDetailResponse>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "no authentication context"));
        }

        if (!Guid.TryParse(caller.UserId, out var ownerUserId))
        {
            return GenericResult<DataverseDetailResponse>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "UserId is not a Guid"));
        }

        var config = new DataverseImplementationConfiguration
        {
            Id = Guid.CreateVersion7(),
            // The implementation the domain provider is registered under. A new record names it or the
            // save refuses it: Save looks the name up before writing, and '' is registered for nothing.
            Implementation = "Dataverse",
            // Why stamped here rather than left to the column DEFAULT: MsSqlInsertTranslator writes
            // every mapped column explicitly, including ones nobody set, so the unset CLR default
            // (0001-01-01) overwrites DEFAULT (sysdatetimeoffset()) rather than deferring to it. Same
            // reason CreateDataverseNoteEndpointBase already stamps its own CreateDate. (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
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

        var saved = await _provider.Save(config, "Dataverse", config.Implementation, config.Name, ct).ConfigureAwait(false);

        // Map what we persisted, not the result's value: Save succeeded on this object, so it is
        // the authoritative shape and needs no null dance.
        return saved.IsFailure
            ? saved.ToNewResult<DataverseDetailResponse>()
            : GenericResult<DataverseDetailResponse>.Success(DataverseResponseMapper.ToDetail(config));
    }
}
