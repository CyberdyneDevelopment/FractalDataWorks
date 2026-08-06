namespace Fdw.Services.Users.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Users.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for user management.
/// </summary>
public class UserApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserApiClient"/> class.
    /// </summary>
    public UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets a list of all users.
    /// </summary>
    /// <returns>A result containing the list of user summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<UserSummaryPayload>>> GetUsers(CancellationToken ct = default)
        => GetList<UserSummaryPayload>("users", ct);

    /// <summary>
    /// Gets a specific user by their unique identifier.
    /// </summary>
    /// <returns>A result containing the user detail.</returns>
    public virtual Task<IGenericResult<UserDetailPayload>> GetUser(Guid id, CancellationToken ct = default)
        => Get<UserDetailPayload>($"users/{id}", ct);

    /// <summary>
    /// Gets the detailed information for the currently authenticated user.
    /// </summary>
    /// <returns>A result containing the current user detail.</returns>
    public virtual Task<IGenericResult<UserDetailPayload>> GetCurrentUser(CancellationToken ct = default)
        => Get<UserDetailPayload>("users/me", ct);

    /// <summary>
    /// Creates a new user. Returns the created user as the canonical resource DTO (API-65).
    /// </summary>
    public virtual Task<IGenericResult<UserDetailPayload>> CreateUser(CreateUserRequest request, CancellationToken ct = default)
        => Post<CreateUserRequest, UserDetailPayload>("users", request, ct);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <returns>A result containing the updated user detail.</returns>
    // Why: server route is PUT /users/{Name} (string), not /users/{Guid}. Callers must pass the username.
    public virtual Task<IGenericResult<UserDetailPayload>> UpdateUser(string name, UpdateUserPayload request, CancellationToken ct = default)
        => Put<UpdateUserPayload, UserDetailPayload>($"users/{name}", request, ct);

    /// <summary>
    /// Deletes a specific user.
    /// </summary>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public virtual Task<IGenericResult> DeleteUser(Guid id, CancellationToken ct = default)
        => Delete($"users/{id}", ct);

    /// <summary>
    /// Gets the roles assigned to a specific user.
    /// </summary>
    /// <returns>A result containing the user's roles.</returns>
    public virtual Task<IGenericResult<UserRolesResponse>> GetUserRoles(Guid id, CancellationToken ct = default)
        => Get<UserRolesResponse>($"users/{id}/roles", ct);

    /// <summary>
    /// Assigns a role to a specific user.
    /// </summary>
    /// <returns>A result indicating whether the role assignment succeeded.</returns>
    public virtual Task<IGenericResult> AssignUserRole(Guid id, AssignRoleRequest request, CancellationToken ct = default)
        => Post($"users/{id}/roles", request, ct);

    /// <summary>
    /// Revokes a role from a specific user.
    /// </summary>
    /// <returns>A result indicating whether the role revocation succeeded.</returns>
    public virtual Task<IGenericResult> RevokeUserRole(Guid id, string roleName, CancellationToken ct = default)
        => Delete($"users/{id}/roles/{roleName}", ct);

    /// <summary>
    /// Resets a user's password without requiring their current one (admin operation).
    /// </summary>
    /// <returns>A result indicating whether the reset succeeded.</returns>
    // Why: the server has exposed POST /users/{IdOrName}/reset-password all along, but nothing called
    // it — an admin could not reset a password from the UI at all. Requires the endpoint's users:write
    // policy, which is a LOWER bar than editing the same user (users:delete); see FDW-634.
    public virtual Task<IGenericResult> ResetPassword(Guid id, string newPassword, CancellationToken ct = default)
        => Post($"users/{id}/reset-password", new ResetPasswordPayload { NewPassword = newPassword }, ct);
}
