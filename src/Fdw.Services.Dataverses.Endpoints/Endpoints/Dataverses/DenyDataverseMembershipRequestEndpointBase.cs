using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Declines a pending membership request — a named action, not a field change, so this is
/// a raw POST endpoint rather than a <c>CrudUpdateEndpointBase</c> derivative.</summary>
public abstract class DenyDataverseMembershipRequestEndpointBase : Endpoint<DenyDataverseMembershipRequestRequest, DataverseMembershipRequestDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly ILogger<DenyDataverseMembershipRequestEndpointBase> _logger;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected DenyDataverseMembershipRequestEndpointBase(
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor authContext,
        ILogger<DenyDataverseMembershipRequestEndpointBase>? logger = null)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _authContext = authContext;
        _logger = logger ?? NullLogger<DenyDataverseMembershipRequestEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/dataverses/{Name}/membership-requests/{RequestId}/deny");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("dataverses:write");
#endif
        Summary(s =>
        {
            s.Summary = "Decline a membership request";
            s.Description = "Marks the request Declined. No membership is granted.";
        });
        Tags("Dataverses");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DenyDataverseMembershipRequestRequest req, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(req.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) { await SendError(dataverse, ct).ConfigureAwait(false); return; }
        if (dataverse.Value is null) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }

        var permitted = await _access.MayWrite(dataverse.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) { await SendError(permitted, ct).ConfigureAwait(false); return; }

        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var reviewedByUserId))
        {
            await SendError(GenericResult.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), _logger,
                ResultDetails.Create("name", req.Name, "reason", "the caller could not be resolved")), ct).ConfigureAwait(false);
            return;
        }

        var lookup = await DataverseMembershipRequestLookup.FindPending(
            Gateway, _dataverses, _logger, req.Name, dataverse.Value.Id, req.RequestId, ct).ConfigureAwait(false);
        if (lookup.IsFailure) { await SendError(lookup, ct).ConfigureAwait(false); return; }

        var found = lookup.Value!;
        found.Status = "Declined";
        found.ReviewedByUserId = reviewedByUserId;
        found.ReviewedAt = DateTimeOffset.UtcNow;
        found.ReviewNotes = req.ReviewNotes;

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMembershipRequest");
        var saveResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseMembershipRequestConfiguration>(found), target, ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure) { await SendError(saveResult, ct).ConfigureAwait(false); return; }

        await Send.OkAsync(DataverseMembershipRequestDtos.From(found), ct).ConfigureAwait(false);
    }

    private Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }
}
