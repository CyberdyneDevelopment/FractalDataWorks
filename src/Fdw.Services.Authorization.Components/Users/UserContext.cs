#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Users.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Authorization.Components.Users;

public sealed class UserContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<UserSummaryPayload> Users { get; init; } = [];
    public string SearchString { get; init; } = string.Empty;

    // ── Derived ────────────────────────────────────────────────────────────────

    public IReadOnlyList<UserSummaryPayload> FilteredUsers { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadUsers { get; init; } = () => Task.CompletedTask;
    public Func<string, Task> OnSearchChanged { get; init; } = _ => Task.CompletedTask;
    public Func<CreateUserRequest, Task<UserDetailPayload?>> OnCreateUser { get; init; } = _ => Task.FromResult<UserDetailPayload?>(null);
    public Func<Guid, UpdateUserPayload, IEnumerable<string>, IEnumerable<string>, Task<bool>> OnUpdateUser { get; init; } = (_, _, _, _) => Task.FromResult(false);
    public Func<Guid, Task<bool>> OnDeleteUser { get; init; } = _ => Task.FromResult(false);

    /// <summary>
    /// Resets the given user's password without requiring their current one (admin operation).
    /// </summary>
    public Func<Guid, string, Task<bool>> OnResetPassword { get; init; } = (_, _) => Task.FromResult(false);
}
