using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>Approves a pending membership request — a named action, not a field change, so this is
/// a raw POST endpoint rather than a <c>CrudUpdateEndpointBase</c> derivative.</summary>
/// <remarks>Grants membership in the same call: marks the request Approved and inserts (or
/// activates) the corresponding <see cref="DataverseMemberConfiguration"/> row, the same shape
/// <see cref="RequestDataverseMembershipEndpointBase"/> uses for an AutoApprove policy.</remarks>
public abstract class ApproveDataverseMembershipRequestEndpointBase : Endpoint<ApproveDataverseMembershipRequestRequest, DataverseMembershipRequestDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly ILogger<ApproveDataverseMembershipRequestEndpointBase> _logger;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected ApproveDataverseMembershipRequestEndpointBase(
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor authContext,
        ILogger<ApproveDataverseMembershipRequestEndpointBase>? logger = null)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _authContext = authContext;
        _logger = logger ?? NullLogger<ApproveDataverseMembershipRequestEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/dataverses/{Name}/membership-requests/{RequestId}/approve");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("dataverses:write");
#endif
        Summary(s =>
        {
            s.Summary = "Approve a membership request";
            s.Description = "Grants the requested membership and marks the request Approved.";
        });
        Tags("Dataverses");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ApproveDataverseMembershipRequestRequest req, CancellationToken ct)
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
        var now = DateTimeOffset.UtcNow;
        found.Status = "Approved";
        found.ReviewedByUserId = reviewedByUserId;
        found.ReviewedAt = now;
        found.ReviewNotes = req.ReviewNotes;

        var requestTarget = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMembershipRequest");
        var saveResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseMembershipRequestConfiguration>(found), requestTarget, ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure) { await SendError(saveResult, ct).ConfigureAwait(false); return; }

        var granted = await GrantMembership(dataverse.Value.Id, found, reviewedByUserId, now, ct).ConfigureAwait(false);
        if (granted.IsFailure) { await SendError(granted, ct).ConfigureAwait(false); return; }

        await Send.OkAsync(DataverseMembershipRequestDtos.From(found), ct).ConfigureAwait(false);
    }

    /// <summary>Inserts the DataverseMember row, unless the subject already holds a current one.</summary>
    /// <remarks>
    /// Already a member covers re-approving after the subject left and rejoined via a fresh request:
    /// the request is Approved either way, but a second insert would collide with
    /// UX_DataverseMember_Dataverse_Subject_Current.
    /// </remarks>
    private async Task<IGenericResult> GrantMembership(
        Guid dataverseImplementationId, DataverseMembershipRequestConfiguration approved, Guid grantedByUserId, DateTimeOffset now, CancellationToken ct)
    {
        var memberTarget = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMember");
        var memberCheckCommand = new QueryCommand<DataverseMemberConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverseImplementationId },
                        new FilterCondition { PropertyName = "SubjectType", Operator = FilterOperators.ByName("Equal"), Value = approved.SubjectType },
                        new FilterCondition { PropertyName = "SubjectId", Operator = FilterOperators.ByName("Equal"), Value = approved.SubjectId },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existingMember = await Gateway.Execute<IEnumerable<DataverseMemberConfiguration>>(memberCheckCommand, memberTarget, ct)
            .ConfigureAwait(false);
        if (existingMember.IsFailure) return existingMember;
        if (existingMember.Value?.Any() == true) return GenericResult.Success();

        var member = new DataverseMemberConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = $"{approved.SubjectType}:{approved.SubjectId}",
            DataverseImplementationId = dataverseImplementationId,
            SubjectType = approved.SubjectType,
            SubjectId = approved.SubjectId,
            MemberRole = approved.RequestedRole,
            State = "Active",
            JoinedAt = now,
            InvitedByUserId = grantedByUserId,
            CreateDate = now,
        };

        var memberResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseMemberConfiguration>(member), memberTarget, ct)
            .ConfigureAwait(false);
        return memberResult.IsFailure ? memberResult : GenericResult.Success();
    }

    private Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }
}
