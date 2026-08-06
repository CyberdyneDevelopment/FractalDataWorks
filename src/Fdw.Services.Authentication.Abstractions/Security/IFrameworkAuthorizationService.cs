using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Provides role-based authorization services for validating user permissions and access rights.
/// This service handles authorization decisions based on user roles, permissions, and resource access patterns.
/// </summary>
/// <remarks>
/// <para>
/// The authorization service operates on authenticated contexts provided by IAuthenticationService
/// and makes access control decisions based on role-based access control (RBAC) principles.
/// It supports fine-grained permissions and hierarchical role structures.
/// </para>
/// <para>
/// Authorization decisions are made using a combination of:
/// - User roles assigned through IRoleProvider
/// - Resource-specific permissions
/// - Action-based access controls
/// - Custom authorization policies
/// </para>
/// <para>
/// All operations return IGenericResult to provide consistent error handling and
/// success/failure semantics across the Fdw framework.
/// </para>
/// <para>
/// This interface should be registered for dependency injection as a singleton
/// or scoped service depending on the authorization provider's caching and
/// performance characteristics.
/// </para>
/// </remarks>
public interface IFrameworkAuthorizationService
{
    /// <summary>
    /// Determines whether the authenticated user has permission to perform the specified action on the given resource.
    /// </summary>
    /// <param name="context">
    /// The authentication context containing user identity and claims.
    /// Cannot be null and must represent an authenticated user.
    /// </param>
    /// <param name="resource">
    /// The resource being accessed. This can be a REST endpoint path, resource name,
    /// or any identifier that represents a protected resource.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="action">
    /// The action being performed on the resource (e.g., "read", "write", "delete", "execute").
    /// Cannot be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the authorization operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous authorization operation.
    /// The task result contains an IGenericResult with a boolean value indicating
    /// whether the user is authorized (true) or not authorized (false) for the specified action.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive authorization checking including:
    /// - Role-based access control validation
    /// - Resource-specific permission verification
    /// - Action-level authorization
    /// - Custom policy evaluation
    /// - Hierarchical role inheritance
    /// </para>
    /// <para>
    /// The authorization decision process typically involves:
    /// 1. Extracting user roles from the authentication context
    /// 2. Resolving permissions associated with those roles
    /// 3. Matching resource and action patterns
    /// 4. Applying any custom authorization policies
    /// 5. Making the final access decision
    /// </para>
    /// <para>
    /// Resource and action strings support pattern matching and wildcards
    /// depending on the implementation (e.g., "/api/users/*" or "data:*").
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="context"/>, <paramref name="resource"/>, or <paramref name="action"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="resource"/> or <paramref name="action"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when <paramref name="context"/> represents an unauthenticated user.
    /// </exception>
    Task<IGenericResult<bool>> Authorize(IAuthenticationContext context, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the authenticated user has the specified role assigned.
    /// </summary>
    /// <param name="context">
    /// The authentication context containing user identity and claims.
    /// Cannot be null and must represent an authenticated user.
    /// </param>
    /// <param name="role">
    /// The role name to check for assignment to the user.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the role check operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous role check operation.
    /// The task result contains an IGenericResult with a boolean value indicating
    /// whether the user has the specified role (true) or not (false).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks for direct role assignment and may also consider
    /// role inheritance hierarchies where supported by the implementation.
    /// For example, a user with an "Admin" role might automatically have
    /// "User" role privileges through inheritance.
    /// </para>
    /// <para>
    /// Role checking can be performed using:
    /// - Direct role assignment verification
    /// - Role hierarchy traversal
    /// - Cached role information from the authentication context
    /// - Real-time role provider queries
    /// </para>
    /// <para>
    /// Role names are typically case-sensitive, though this behavior
    /// depends on the specific role provider implementation.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="context"/> or <paramref name="role"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="role"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when <paramref name="context"/> represents an unauthenticated user.
    /// </exception>
    Task<IGenericResult<bool>> HasRole(IAuthenticationContext context, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the authenticated user has the specified permission.
    /// </summary>
    /// <param name="context">
    /// The authentication context containing user identity and claims.
    /// Cannot be null and must represent an authenticated user.
    /// </param>
    /// <param name="permission">
    /// The permission name to check for assignment to the user.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the permission check operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous permission check operation.
    /// The task result contains an IGenericResult with a boolean value indicating
    /// whether the user has the specified permission (true) or not (false).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Permission checking provides fine-grained access control beyond role-based
    /// authorization. Permissions are typically more specific than roles and
    /// represent atomic operations or access rights.
    /// </para>
    /// <para>
    /// Permissions can be assigned:
    /// - Directly to users
    /// - Through role membership
    /// - Via group or organizational unit membership
    /// - Through dynamic policy evaluation
    /// </para>
    /// <para>
    /// Common permission patterns include:
    /// - "users.create", "users.read", "users.update", "users.delete"
    /// - "reports.view", "reports.export"
    /// - "admin.system", "admin.users"
    /// </para>
    /// <para>
    /// The permission resolution process considers all sources of permissions
    /// including direct assignment, role-based inheritance, and group membership.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="context"/> or <paramref name="permission"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="permission"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when <paramref name="context"/> represents an unauthenticated user.
    /// </exception>
    Task<IGenericResult<bool>> HasPermission(IAuthenticationContext context, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all roles assigned to the authenticated user.
    /// </summary>
    /// <param name="context">
    /// The authentication context containing user identity and claims.
    /// Cannot be null and must represent an authenticated user.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the role retrieval operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous role retrieval operation.
    /// The task result contains an IGenericResult with an enumerable collection of role names
    /// assigned to the user, or an empty collection if no roles are assigned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns all roles directly assigned to the user and may also
    /// include inherited roles from role hierarchies, depending on the implementation.
    /// The returned collection represents the complete set of roles that apply
    /// to the user for authorization decisions.
    /// </para>
    /// <para>
    /// Role information can be obtained from:
    /// - Cached data in the authentication context
    /// - Real-time queries to the role provider
    /// - Combined local and remote role sources
    /// </para>
    /// <para>
    /// The returned collection maintains the original role names as stored
    /// in the role provider, preserving case sensitivity and exact formatting.
    /// </para>
    /// <para>
    /// For performance considerations, implementations may cache role information
    /// within the authentication context or use other optimization strategies.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when <paramref name="context"/> represents an unauthenticated user.
    /// </exception>
    Task<IGenericResult<IEnumerable<string>>> GetRoles(IAuthenticationContext context, CancellationToken cancellationToken = default);
}
