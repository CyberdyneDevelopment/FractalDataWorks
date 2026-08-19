using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Resolves the permission set granted by a set of ROLE NAMES, independent of any user assignment.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IEffectivePermissionResolver"/> answers "what may this user do", and reaches the role
/// catalogue only through <c>authz.UserRole</c>. A principal authenticated by an external issuer has
/// no <c>usr.Users</c> row and therefore no role assignment, so that resolver reports an empty set for
/// it — correctly, since the question it answers does not apply.
/// </para>
/// <para>
/// This resolver answers the other question the authorization domain owns: "what does holding these
/// roles grant". The roles come from whatever established the principal — an authentication service's
/// declared roles, a role claim mapped off an external token — and the expansion through
/// <c>authz.RolePermission</c> is the same one the user path uses, so the two never diverge.
/// </para>
/// </remarks>
public interface IRolePermissionResolver
{
    /// <summary>
    /// Resolves the union of permissions granted by <paramref name="roleNames"/>.
    /// </summary>
    /// <param name="roleNames">The role names to expand. Every name must exist in <c>authz.Role</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// On success, the union of the permissions those roles grant. Failure when the catalogue could not
    /// be read or when a named role does not exist — an unknown role name is a configuration fault, not
    /// an empty grant, and callers must treat failure as "deny".
    /// </returns>
    Task<IGenericResult<IReadOnlyCollection<string>>> Resolve(
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default);
}
