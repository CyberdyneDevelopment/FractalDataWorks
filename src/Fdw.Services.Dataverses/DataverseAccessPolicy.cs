using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>
/// Decides dataverse write access from ownership and membership.
/// </summary>
/// <remarks>
/// <para>
/// Two ways to be allowed: own it, or hold a membership that is both in effect and carries a role
/// that may write. Both halves are read off their options — <see cref="DataverseMemberStateBase"/>
/// says whether someone is here, <see cref="DataverseMemberRoleBase"/> says what they may do — so a
/// state or role added later arrives with its own answer rather than falling through a branch here.
/// </para>
/// <para>
/// Fail-closed throughout. An unresolvable caller, an unreadable role list, a membership naming a
/// state or role this deployment does not know: each is a refusal with the reason, never an
/// assumption that it was probably fine.
/// </para>
/// <para>
/// Role memberships match against the roles baked into the caller's token, which is the same set
/// every other permission check reads. That set is resolved at sign-in, so a role revoked mid-session
/// keeps its dataverse access until the token is renewed — the same window every baked permission
/// has, not a property of this policy.
/// </para>
/// </remarks>
public sealed class DataverseAccessPolicy : IDataverseAccessPolicy
{
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly RoleConfigurationProvider _roles;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataverseAccessPolicy"/> class.</summary>
    /// <param name="authContext">Supplies the calling user.</param>
    /// <param name="roles">Resolves the caller's role names to the ids a membership records.</param>
    /// <param name="logger">The logger.</param>
    public DataverseAccessPolicy(
        IAuthenticationContextAccessor authContext,
        RoleConfigurationProvider roles,
        ILogger<DataverseAccessPolicy>? logger = null)
    {
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _logger = logger ?? NullLogger<DataverseAccessPolicy>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> MayWrite(
        DataverseImplementationConfiguration dataverse, CancellationToken cancellationToken = default)
    {
        if (dataverse is null) throw new ArgumentNullException(nameof(dataverse));

        if (_authContext.Current is not { } caller)
            return Deny(dataverse.Name, "no authentication context");

        if (!Guid.TryParse(caller.UserId, out var callerUserId))
            return Deny(dataverse.Name, "the caller's UserId is not a Guid");

        if (dataverse.OwnerUserId == callerUserId)
            return GenericResult.Success();

        var callerRoleIds = await ResolveRoleIds(caller, cancellationToken).ConfigureAwait(false);
        if (callerRoleIds.IsFailure || callerRoleIds.Value is null)
            return Deny(dataverse.Name, "the caller's roles could not be read");

        return dataverse.Members.Any(m => Permits(m, callerUserId, callerRoleIds.Value))
            ? GenericResult.Success()
            : Deny(dataverse.Name, "the caller neither owns it nor holds a membership that may change it");
    }

    // Every clause is a reason to refuse, so an unknown state or role name denies rather than
    // being skipped past: a membership this deployment cannot interpret is not a membership it
    // should honour.
    private static bool Permits(
        DataverseMemberConfiguration member, Guid callerUserId, IReadOnlyCollection<Guid> callerRoleIds)
    {
        if (!member.IsCurrent || member.IsDeleted)
            return false;

        if (DataverseMemberStates.ByName(member.State) is not { GrantsMembership: true })
            return false;

        if (DataverseMemberRoles.ByName(member.MemberRole) is not { MayWrite: true })
            return false;

        return DataverseSubjectTypes.ByName(member.SubjectType)
            .Holds(member.SubjectId, callerUserId, callerRoleIds);
    }

    private async Task<IGenericResult<IReadOnlyCollection<Guid>>> ResolveRoleIds(
        IAuthenticationContext caller, CancellationToken cancellationToken)
    {
        // Read through Get rather than GetAllRoles: GetAllRoles answers an unreadable store with an
        // empty list, which here would read as "this caller holds no roles" and deny for the wrong
        // reason. The refusal is the same either way; what the operator is told is not.
        var all = await _roles.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure || all.Value is null)
            return all.ToNewResult<IReadOnlyCollection<Guid>>();

        return GenericResult<IReadOnlyCollection<Guid>>.Success(
            [.. all.Value
                .Where(r => r.IsCurrent && !r.IsDeleted && caller.Roles.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Id)]);
    }

    private IGenericResult Deny(string dataverseName, string reason)
        => GenericResult.Failure(
            DataversesResultCodes.ByName("DataverseWriteNotPermitted"), _logger,
            ResultDetails.Create("name", dataverseName, "reason", reason));
}
