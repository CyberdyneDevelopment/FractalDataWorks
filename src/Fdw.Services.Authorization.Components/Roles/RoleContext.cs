using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Authorization.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Authorization.Components.Roles;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="RoleProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class RoleContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of roles.</summary>
    public IReadOnlyList<RoleSummaryPayload> Roles { get; init; } = [];

    /// <summary>Gets the grouped permission definitions.</summary>
    public IReadOnlyList<PermissionGroupPayload> PermissionGroups { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all roles.</summary>
    public Func<Task> OnLoadRoles { get; init; } = () => Task.CompletedTask;

    /// <summary>Loads all permission groups.</summary>
    public Func<Task> OnLoadPermissionGroups { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets details for a specific role by name.</summary>
    public Func<string, Task<RoleDetailPayload?>> OnGetRoleDetails { get; init; } = _ => Task.FromResult<RoleDetailPayload?>(null);

    /// <summary>Creates a new role.</summary>
    public Func<CreateRolePayload, Task<RoleDetailPayload?>> OnCreateRole { get; init; } = _ => Task.FromResult<RoleDetailPayload?>(null);

    /// <summary>Updates an existing role.</summary>
    public Func<string, UpdateRolePayload, Task<RoleDetailPayload?>> OnUpdateRole { get; init; } = (_, _) => Task.FromResult<RoleDetailPayload?>(null);

    /// <summary>Deletes a role by name.</summary>
    public Func<string, Task<bool>> OnDeleteRole { get; init; } = _ => Task.FromResult(false);

    /// <summary>Saves permissions for a role.</summary>
    public Func<string, List<string>, Task<bool>> OnSavePermissions { get; init; } = (_, _) => Task.FromResult(false);
}
