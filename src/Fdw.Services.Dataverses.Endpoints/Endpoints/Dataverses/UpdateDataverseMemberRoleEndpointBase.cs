using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Results;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Changes the role a member holds on a dataverse.</summary>
/// <remarks>
/// Version-on-write via <see cref="ConfigurationSaveCommand{T}"/>, same as
/// <see cref="UpdateDataverseRelationshipEndpointBase"/>, carrying every other column forward from
/// the loaded row so only MemberRole and ModifyDate move. FDW-798 means the row's true CreateDate is
/// currently lost on this save regardless of what this endpoint sets -- filed, not fixed here.
/// </remarks>
public abstract class UpdateDataverseMemberRoleEndpointBase
    : CrudUpdateEndpointBase<UpdateDataverseMemberRoleRequest, DataverseMemberDto>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    // Set by FindForUpdate, read by Update -- both run within the same request via
    // CrudUpdateEndpointBase.HandleAsync, in that order, on a per-request endpoint instance.
    private DataverseMemberConfiguration? _existing;

    /// <inheritdoc />
    protected UpdateDataverseMemberRoleEndpointBase(
        ILogger<UpdateDataverseMemberRoleEndpointBase> logger,
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways) : base(logger)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/members/{MemberId}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Change a member's role";

    /// <inheritdoc />
    protected override string EndpointDescription => "Changes the role a member holds on the dataverse.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateDataverseMemberRoleRequest request)
        => request.MemberId.ToString();

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMemberDto?>> FindForUpdate(
        UpdateDataverseMemberRoleRequest request, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseMemberDto?>();
        if (dataverse.Value is null) return GenericResult<DataverseMemberDto?>.Success(null);

        var permitted = await _access.MayWrite(dataverse.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<DataverseMemberDto?>();

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
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = request.MemberId },
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverse.Value.Id },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await Gateway.Execute<IEnumerable<DataverseMemberConfiguration>>(existingCommand, target, ct)
            .ConfigureAwait(false);
        if (existing.IsFailure) return existing.ToNewResult<DataverseMemberDto?>();

        _existing = existing.Value?.FirstOrDefault();
        return GenericResult<DataverseMemberDto?>.Success(_existing is null ? null : ToDto(_existing));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseMemberDto>> Update(
        UpdateDataverseMemberRoleRequest request, DataverseMemberDto existing, CancellationToken ct)
    {
        // FindForUpdate always runs before Update in CrudUpdateEndpointBase.HandleAsync, and a null
        // _existing there already short-circuits to 404 before Update is ever called.
        var row = _existing!;

        if (ReferenceEquals(DataverseMemberRoles.ByName(request.MemberRole), DataverseMemberRoles.NotFound))
        {
            return GenericResult<DataverseMemberDto>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "memberRole", "value", request.MemberRole));
        }

        row.MemberRole = request.MemberRole;
        // Why stamped here: MsSqlConfigurationSaveTranslator's INSERT half writes every mapped
        // column explicitly, same as the create/attach sites (FDW-793) -- CreateDate is left as the
        // value already on `row`, though FDW-798 means that value is dropped regardless of what it
        // is (the translator excludes it from the INSERT unconditionally), not just here.
        row.ModifyDate = DateTimeOffset.UtcNow;

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMember");
        var saveResult = await Gateway.Execute<int>(
            new ConfigurationSaveCommand<DataverseMemberConfiguration>(row), target, ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure) return saveResult.ToNewResult<DataverseMemberDto>();

        return GenericResult<DataverseMemberDto>.Success(ToDto(row));
    }

    private static DataverseMemberDto ToDto(DataverseMemberConfiguration member) => new()
    {
        Id = member.Id,
        SubjectType = member.SubjectType,
        SubjectId = member.SubjectId,
        MemberRole = member.MemberRole,
        State = member.State,
        JoinedAt = member.JoinedAt,
    };
}
