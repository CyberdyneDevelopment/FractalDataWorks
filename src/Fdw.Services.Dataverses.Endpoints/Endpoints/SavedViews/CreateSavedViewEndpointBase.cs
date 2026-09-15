using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Creates a saved view.</summary>
/// <remarks>
/// A saved view is a top-level named configuration, like a dataverse, not a dataverse's child --
/// it has no DataverseImplementationId at all. Membership in a dataverse is via
/// DataverseResource, the same as a data set (SavedView is already a registered
/// DataverseResourceKinds option), so attaching this view once created is a separate call to the
/// existing POST /dataverses/{Name}/resources.
/// </remarks>
public abstract class CreateSavedViewEndpointBase : CrudCreateEndpointBase<CreateSavedViewRequest, SavedViewResponse>
{
    private readonly ISavedViewConfigurationProvider _provider;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <inheritdoc />
    protected CreateSavedViewEndpointBase(
        ILogger<CreateSavedViewEndpointBase> logger,
        ISavedViewConfigurationProvider provider,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _provider = provider;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "saved-views";

    /// <inheritdoc />
    protected override string GetResourceName(CreateSavedViewRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateSavedViewRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<bool>()
            : GenericResult<bool>.Success(result.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SavedViewResponse>> Create(
        CreateSavedViewRequest request, CancellationToken ct)
    {
        if (_authContext.Current is not { } caller)
        {
            return GenericResult<SavedViewResponse>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "no authentication context"));
        }

        if (!Guid.TryParse(caller.UserId, out var ownerUserId))
        {
            return GenericResult<SavedViewResponse>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "UserId is not a Guid"));
        }

        var config = new SavedViewImplementationConfiguration
        {
            Id = Guid.CreateVersion7(),
            // The implementation the domain provider is registered under. A new record names it or
            // the save refuses it: Save looks the name up before writing, and '' is registered for
            // nothing.
            Implementation = "SavedView",
            // Why stamped here rather than left to the column DEFAULT: MsSqlInsertTranslator writes
            // every mapped column explicitly, including ones nobody set, so the unset CLR default
            // (0001-01-01) overwrites DEFAULT (sysdatetimeoffset()) rather than deferring to it.
            // (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
            OwnerUserId = ownerUserId,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            SubjectDataSetId = request.SubjectDataSetId,
            Encoding = request.Encoding,
            Filters = request.Filters,
            ChartType = request.ChartType,
        };

        var saved = await _provider.Save(config, "SavedView", config.Implementation, config.Name, ct).ConfigureAwait(false);

        return saved.IsFailure
            ? saved.ToNewResult<SavedViewResponse>()
            : GenericResult<SavedViewResponse>.Success(SavedViewResponseMapper.ToResponse(config));
    }
}
