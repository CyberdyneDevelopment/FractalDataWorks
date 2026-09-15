using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Asks to join a dataverse — honours the dataverse's own <see cref="IDataverseImplementationConfiguration.JoinPolicy"/>.</summary>
/// <remarks>
/// No <see cref="IDataverseAccessPolicy"/> dependency: requesting is the thing someone without write
/// access does, so this never calls MayWrite. A Closed policy is a business refusal read off the
/// dataverse's own configuration, not an authorization decision.
/// </remarks>
public abstract class RequestDataverseMembershipEndpointBase
    : CrudCreateEndpointBase<RequestDataverseMembershipRequest, DataverseMembershipRequestDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authContext;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    // Set by CheckExists when it finds a conflict, read back by DuplicateMessage.
    private DataverseMembershipRequestConfiguration? _conflict;

    /// <inheritdoc />
    protected RequestDataverseMembershipEndpointBase(
        ILogger<RequestDataverseMembershipEndpointBase> logger,
        DataverseConfigurationProvider dataverses,
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _dataverses = dataverses;
        _dataGateways = dataGateways;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/membership-requests";

    /// <inheritdoc />
    protected override string EndpointSummary => "Ask to join a dataverse";

    /// <inheritdoc />
    protected override string EndpointDescription => "Requests membership, honouring the dataverse's join policy.";

    /// <inheritdoc />
    protected override string GetResourceName(RequestDataverseMembershipRequest request) => request.Name;

    /// <inheritdoc />
    /// <remarks>A subject with an already-Pending request cannot ask again until it is reviewed.</remarks>
    protected override async Task<IGenericResult<bool>> CheckExists(RequestDataverseMembershipRequest request, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<bool>();
        if (dataverse.Value is null) return GenericResult<bool>.Success(false);

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMembershipRequest");
        var existingCommand = new QueryCommand<DataverseMembershipRequestConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverse.Value.Id },
                        new FilterCondition { PropertyName = "SubjectType", Operator = FilterOperators.ByName("Equal"), Value = request.SubjectType },
                        new FilterCondition { PropertyName = "SubjectId", Operator = FilterOperators.ByName("Equal"), Value = request.SubjectId },
                        new FilterCondition { PropertyName = "Status", Operator = FilterOperators.ByName("Equal"), Value = "Pending" },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await Gateway.Execute<IEnumerable<DataverseMembershipRequestConfiguration>>(existingCommand, target, ct)
            .ConfigureAwait(false);
        if (existing.IsFailure) return existing.ToNewResult<bool>();

        _conflict = existing.Value?.FirstOrDefault();
        return GenericResult<bool>.Success(_conflict is not null);
    }

    /// <inheritdoc />
    protected override string DuplicateMessage(RequestDataverseMembershipRequest request) =>
        _conflict is null
            ? base.DuplicateMessage(request)
            : $"{_conflict.SubjectType} '{_conflict.SubjectId}' already has a pending request on '{request.Name}'";

    /// <summary>Validates SubjectType and RequestedRole against their closed sets.</summary>
    private IGenericResult ValidateFields(RequestDataverseMembershipRequest request)
    {
        if (ReferenceEquals(DataverseSubjectTypes.ByName(request.SubjectType), DataverseSubjectTypes.NotFound))
        {
            return GenericResult.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "subjectType", "value", request.SubjectType));
        }

        if (ReferenceEquals(DataverseMemberRoles.ByName(request.RequestedRole), DataverseMemberRoles.NotFound))
        {
            return GenericResult.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "requestedRole", "value", request.RequestedRole));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMembershipRequestDto>> Create(
        RequestDataverseMembershipRequest request, CancellationToken ct)
    {
        var validated = ValidateFields(request);
        if (validated.IsFailure) return validated.ToNewResult<DataverseMembershipRequestDto>();

        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseMembershipRequestDto>();
        if (dataverse.Value is null)
        {
            return GenericResult<DataverseMembershipRequestDto>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var policy = DataverseJoinPolicies.ByName(dataverse.Value.JoinPolicy);
        if (ReferenceEquals(policy, DataverseJoinPolicies.NotFound) || !policy.AcceptsRequests)
        {
            return GenericResult<DataverseMembershipRequestDto>.Failure(
                DataversesResultCodes.ByName("DataverseJoinPolicyRefused"), Logger,
                ResultDetails.Create("name", request.Name, "reason", $"'{dataverse.Value.JoinPolicy}' does not accept membership requests"));
        }

        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var requestedByUserId))
        {
            return GenericResult<DataverseMembershipRequestDto>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the caller could not be resolved"));
        }

        var now = DateTimeOffset.UtcNow;
        var autoApproved = policy.AutoApproves;
        var membershipRequest = BuildRequestRow(request, dataverse.Value.Id, requestedByUserId, autoApproved, now);

        var requestTarget = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMembershipRequest");
        var insertResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseMembershipRequestConfiguration>(membershipRequest), requestTarget, ct)
            .ConfigureAwait(false);
        if (insertResult.IsFailure) return insertResult.ToNewResult<DataverseMembershipRequestDto>();

        if (autoApproved)
        {
            var granted = await GrantMembership(dataverse.Value.Id, membershipRequest, requestedByUserId, now, ct).ConfigureAwait(false);
            if (granted.IsFailure) return granted.ToNewResult<DataverseMembershipRequestDto>();
        }

        return GenericResult<DataverseMembershipRequestDto>.Success(DataverseMembershipRequestDtos.From(membershipRequest));
    }

    private static DataverseMembershipRequestConfiguration BuildRequestRow(
        RequestDataverseMembershipRequest request, Guid dataverseImplementationId, Guid requestedByUserId, bool autoApproved, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = $"{request.SubjectType}:{request.SubjectId}:{dataverseImplementationId}",
        DataverseImplementationId = dataverseImplementationId,
        RequestedByUserId = requestedByUserId,
        SubjectType = request.SubjectType,
        SubjectId = request.SubjectId,
        RequestedRole = request.RequestedRole,
        Justification = request.Justification,
        Status = autoApproved ? "Approved" : "Pending",
        ReviewedByUserId = autoApproved ? requestedByUserId : null,
        ReviewedAt = autoApproved ? now : null,
        ReviewNotes = autoApproved ? "Auto-approved by the dataverse's join policy" : null,
        // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
        // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
        // than deferring to it. (FDW-793)
        CreateDate = now,
    };

    private async Task<IGenericResult> GrantMembership(
        Guid dataverseImplementationId, DataverseMembershipRequestConfiguration approved, Guid grantedByUserId, DateTimeOffset now, CancellationToken ct)
    {
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

        var memberTarget = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMember");
        var memberResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseMemberConfiguration>(member), memberTarget, ct)
            .ConfigureAwait(false);
        return memberResult.IsFailure ? memberResult : GenericResult.Success();
    }
}
