using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Lists a dataverse's members.</summary>
/// <remarks>
/// Reads through the aggregate's composed Members collection, like <see cref="GetDataverseMapEndpointBase"/>
/// reads Resources/Relationships -- FDW-795's child-join fix already filters the child rows' own
/// IsCurrent/IsDeleted, so this is not the insert-only-cascade concern that keeps Attach/Create off
/// the aggregate's Save path.
/// </remarks>
public abstract class ListDataverseMembersEndpointBase
    : CrudListEndpointBase<DataverseNameRequest, DataverseMemberDto>
{
    private readonly IDataverseConfigurationProvider _dataverses;

    /// <inheritdoc />
    protected ListDataverseMembersEndpointBase(
        ILogger<ListDataverseMembersEndpointBase> logger,
        IDataverseConfigurationProvider dataverses) : base(logger)
    {
        _dataverses = dataverses;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/members";

    /// <inheritdoc />
    protected override string EndpointSummary => "List a dataverse's members";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns everyone and every role holding membership in the dataverse.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataverseMemberDto>>> LoadItems(
        DataverseNameRequest request, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<List<DataverseMemberDto>>();
        if (dataverse.Value is null) return GenericResult<List<DataverseMemberDto>>.Success([]);

        var members = dataverse.Value.Members
            .Select(m => new DataverseMemberDto
            {
                Id = m.Id,
                SubjectType = m.SubjectType,
                SubjectId = m.SubjectId,
                MemberRole = m.MemberRole,
                State = m.State,
                JoinedAt = m.JoinedAt,
            })
            .ToList();

        return GenericResult<List<DataverseMemberDto>>.Success(members);
    }
}
