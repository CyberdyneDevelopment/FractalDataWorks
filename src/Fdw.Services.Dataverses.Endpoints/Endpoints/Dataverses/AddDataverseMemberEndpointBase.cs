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

/// <summary>Adds a member to a dataverse — grants a user or role a place on it.</summary>
/// <remarks>
/// Inserted directly through the gateway, not by loading the aggregate and re-saving it -- same
/// reason as <see cref="AttachDataverseResourceEndpointBase"/>: ImplementationProviderBase.Save's
/// cascade has no update/diff path, so re-saving an aggregate whose Members list is already
/// populated would duplicate every one of them.
/// </remarks>
public abstract class AddDataverseMemberEndpointBase
    : CrudCreateEndpointBase<AddDataverseMemberRequest, DataverseMemberDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authContext;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    // Set by CheckExists when it finds a conflict, read back by DuplicateMessage.
    private DataverseMemberConfiguration? _conflict;

    /// <inheritdoc />
    protected AddDataverseMemberEndpointBase(
        ILogger<AddDataverseMemberEndpointBase> logger,
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _authContext = authContext;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/members";

    /// <inheritdoc />
    protected override string EndpointSummary => "Add a member to a dataverse";

    /// <inheritdoc />
    protected override string EndpointDescription => "Grants a user or role membership of the dataverse.";

    /// <inheritdoc />
    protected override string GetResourceName(AddDataverseMemberRequest request) => request.Name;

    /// <inheritdoc />
    /// <remarks>
    /// UX_DataverseMember_Dataverse_Subject_Current is unique on (DataverseImplementationId,
    /// SubjectType, SubjectId) WHERE IsCurrent = 1 -- a subject holds at most one role at a time.
    /// </remarks>
    protected override async Task<IGenericResult<bool>> CheckExists(AddDataverseMemberRequest request, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<bool>();
        if (dataverse.Value is null) return GenericResult<bool>.Success(false);

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMember");
        var existingCommand = new QueryCommand<DataverseMemberConfiguration>
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
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await Gateway.Execute<IEnumerable<DataverseMemberConfiguration>>(existingCommand, target, ct)
            .ConfigureAwait(false);
        if (existing.IsFailure) return existing.ToNewResult<bool>();

        _conflict = existing.Value?.FirstOrDefault();
        return GenericResult<bool>.Success(_conflict is not null);
    }

    /// <inheritdoc />
    protected override string DuplicateMessage(AddDataverseMemberRequest request) =>
        _conflict is null
            ? base.DuplicateMessage(request)
            : $"{_conflict.SubjectType} '{_conflict.SubjectId}' already holds {_conflict.MemberRole} on '{request.Name}'";

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMemberDto>> Create(
        AddDataverseMemberRequest request, CancellationToken ct)
    {
        if (ReferenceEquals(DataverseSubjectTypes.ByName(request.SubjectType), DataverseSubjectTypes.NotFound))
        {
            return GenericResult<DataverseMemberDto>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "subjectType", "value", request.SubjectType));
        }

        if (ReferenceEquals(DataverseMemberRoles.ByName(request.MemberRole), DataverseMemberRoles.NotFound))
        {
            return GenericResult<DataverseMemberDto>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "memberRole", "value", request.MemberRole));
        }

        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseMemberDto>();
        if (dataverse.Value is null)
        {
            return GenericResult<DataverseMemberDto>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var permitted = await _access.MayWrite(dataverse.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<DataverseMemberDto>();

        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var invitedByUserId))
        {
            return GenericResult<DataverseMemberDto>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the caller could not be resolved"));
        }

        var joinedAt = DateTimeOffset.UtcNow;
        var member = new DataverseMemberConfiguration
        {
            Id = Guid.CreateVersion7(),
            // The column is NOT NULL but carries no identity beyond the subject it names -- the
            // membership IS the row, so its Name is derived from what it grants.
            Name = $"{request.SubjectType}:{request.SubjectId}",
            DataverseImplementationId = dataverse.Value.Id,
            SubjectType = request.SubjectType,
            SubjectId = request.SubjectId,
            MemberRole = request.MemberRole,
            // Added directly by a caller who already has dataverses:write on this dataverse -- this
            // is a grant, not an invitation, so State starts Active rather than Invited. A2b (FDW-727
            // A4: membership requests) is the separate self-service join flow.
            State = "Active",
            JoinedAt = joinedAt,
            InvitedByUserId = invitedByUserId,
            // Why stamped here: MsSqlInsertTranslator writes every mapped column explicitly, so the
            // unset CLR default (0001-01-01) would overwrite DEFAULT (sysdatetimeoffset()) rather
            // than deferring to it. (FDW-793)
            CreateDate = DateTimeOffset.UtcNow,
        };

        // ConfigurationSaveCommand, not InsertCommand -- see AttachDataverseResourceEndpointBase for
        // why: DataverseImplementationRowId is a physical FK with no matching C# property.
        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMember");
        var insertResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseMemberConfiguration>(member), target, ct)
            .ConfigureAwait(false);
        if (insertResult.IsFailure) return insertResult.ToNewResult<DataverseMemberDto>();

        return GenericResult<DataverseMemberDto>.Success(new DataverseMemberDto
        {
            Id = member.Id,
            SubjectType = member.SubjectType,
            SubjectId = member.SubjectId,
            MemberRole = member.MemberRole,
            State = member.State,
            JoinedAt = member.JoinedAt,
        });
    }
}
