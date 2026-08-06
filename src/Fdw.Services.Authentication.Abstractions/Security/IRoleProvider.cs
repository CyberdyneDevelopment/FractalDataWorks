using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Provides role management services for assigning, removing, and querying user roles and role permissions.
/// This service handles the administrative aspects of role-based access control (RBAC) within the security system.
/// </summary>
/// <remarks>
/// <para>
/// The role provider serves as the authoritative source for role management operations,
/// supporting both read and write operations for role assignments and permission mappings.
/// It integrates with authentication and authorization services to provide complete RBAC functionality.
/// </para>
/// <para>
/// Key responsibilities include:
/// - Managing user-to-role assignments
/// - Maintaining role-to-permission mappings
/// - Supporting role hierarchy and inheritance
/// - Providing efficient role query capabilities
/// - Ensuring role assignment consistency and integrity
/// </para>
/// <para>
/// The service is designed to work with various backend storage systems including
/// databases, LDAP directories, cloud identity providers, and custom role stores.
/// </para>
/// <para>
/// All operations return IGenericResult to provide consistent error handling and
/// success/failure semantics across the Fdw framework.
/// </para>
/// <para>
/// Implementations should consider caching strategies for frequently accessed
/// role data to optimize performance in high-volume scenarios.
/// </para>
/// </remarks>
public interface IRoleProvider
{
    /// <summary>
    /// Retrieves all roles assigned to the specified user.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user whose roles should be retrieved.
    /// Cannot be null or empty.
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
    /// include inherited roles from role hierarchies or group memberships,
    /// depending on the implementation configuration.
    /// </para>
    /// <para>
    /// Role retrieval considers:
    /// - Direct role assignments to the user
    /// - Role inheritance through hierarchical relationships
    /// - Group-based role assignments (if supported)
    /// - Time-based role assignments and expiration
    /// - Role activation status and availability
    /// </para>
    /// <para>
    /// The returned collection maintains role names exactly as stored in the
    /// role provider, preserving case sensitivity and formatting. Duplicate
    /// roles should be eliminated from the result set.
    /// </para>
    /// <para>
    /// For users that don't exist in the system, implementations should return
    /// an empty collection rather than an error, allowing for graceful handling
    /// of edge cases in distributed systems.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="userId"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="userId"/> is empty or whitespace.
    /// </exception>
    Task<IGenericResult<IEnumerable<string>>> GetUserRoles(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns the specified role to the user.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user to whom the role should be assigned.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="role">
    /// The name of the role to assign to the user.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the role assignment operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous role assignment operation.
    /// The task result contains an IGenericResult indicating success or failure of the assignment.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Role assignment creates a persistent association between the user and the
    /// specified role, granting the user all permissions associated with that role.
    /// The assignment becomes effective immediately upon successful completion.
    /// </para>
    /// <para>
    /// Assignment behavior includes:
    /// - Idempotent operation - assigning an already-assigned role succeeds without error
    /// - Validation of role existence before assignment
    /// - Verification of user existence and validity
    /// - Audit logging of role assignment activities
    /// - Integration with external identity providers when applicable
    /// </para>
    /// <para>
    /// The operation may fail if:
    /// - The specified role does not exist in the system
    /// - The user does not exist or is inactive
    /// - The user lacks permission to receive the role (policy violations)
    /// - System constraints prevent the assignment (maximum role limits)
    /// - External identity provider integration fails
    /// </para>
    /// <para>
    /// Role assignments should be immediately reflected in subsequent authorization
    /// checks, though implementations may use caching strategies that introduce
    /// brief propagation delays.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="userId"/> or <paramref name="role"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="userId"/> or <paramref name="role"/> is empty or whitespace.
    /// </exception>
    Task<IGenericResult> AssignRole(string userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified role assignment from the user.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user from whom the role should be removed.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="role">
    /// The name of the role to remove from the user.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the role removal operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous role removal operation.
    /// The task result contains an IGenericResult indicating success or failure of the removal.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Role removal eliminates the association between the user and the specified role,
    /// immediately revoking all permissions granted through that role assignment.
    /// The removal becomes effective immediately upon successful completion.
    /// </para>
    /// <para>
    /// Removal behavior includes:
    /// - Idempotent operation - removing a non-assigned role succeeds without error
    /// - Preservation of other role assignments for the user
    /// - Audit logging of role removal activities
    /// - Integration with external identity providers when applicable
    /// - Validation that the role removal doesn't violate system policies
    /// </para>
    /// <para>
    /// The operation may fail if:
    /// - System policies prevent role removal (minimum role requirements)
    /// - External identity provider integration fails
    /// - The user has protected roles that cannot be removed
    /// - Concurrent modification conflicts occur
    /// </para>
    /// <para>
    /// Role removals should be immediately reflected in subsequent authorization
    /// checks, potentially requiring cache invalidation or refresh operations
    /// for optimal security.
    /// </para>
    /// <para>
    /// Care should be taken when removing roles from active user sessions, as
    /// existing authentication contexts may retain role information until
    /// token expiration or session refresh.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="userId"/> or <paramref name="role"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="userId"/> or <paramref name="role"/> is empty or whitespace.
    /// </exception>
    Task<IGenericResult> RemoveRole(string userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all permissions associated with the specified role.
    /// </summary>
    /// <param name="role">
    /// The name of the role whose permissions should be retrieved.
    /// Cannot be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the permission retrieval operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous permission retrieval operation.
    /// The task result contains an IGenericResult with an enumerable collection of permission names
    /// associated with the role, or an empty collection if no permissions are assigned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns all permissions directly associated with the specified role
    /// and may also include inherited permissions from parent roles or role hierarchies,
    /// depending on the implementation configuration.
    /// </para>
    /// <para>
    /// Permission retrieval includes:
    /// - Direct permission assignments to the role
    /// - Inherited permissions from parent/super roles
    /// - Calculated permissions based on role policies
    /// - Active permissions (excluding expired or disabled permissions)
    /// </para>
    /// <para>
    /// The returned permission collection is used by authorization services to
    /// make access control decisions and by administrative interfaces to display
    /// role capabilities.
    /// </para>
    /// <para>
    /// Common permission naming conventions include:
    /// - Resource.Action format (e.g., "users.create", "reports.read")
    /// - Hierarchical permissions (e.g., "admin.users.manage")
    /// - Domain-specific permissions (e.g., "finance.budget.approve")
    /// </para>
    /// <para>
    /// For roles that don't exist in the system, implementations should return
    /// an empty collection to support graceful degradation and prevent
    /// authorization failures.
    /// </para>
    /// <para>
    /// The results may be cached for performance, but implementations should
    /// ensure that permission changes are reflected in a timely manner for
    /// security requirements.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="role"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="role"/> is empty or whitespace.
    /// </exception>
    Task<IGenericResult<IEnumerable<string>>> GetRolePermissions(string role, CancellationToken cancellationToken = default);
}
