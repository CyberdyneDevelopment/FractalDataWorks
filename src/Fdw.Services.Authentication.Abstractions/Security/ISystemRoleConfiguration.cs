using System.Security.Claims;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Provides the system-level role names and role-check operations resolved from configurable
/// deployment settings rather than hardcoded strings.
/// </summary>
/// <remarks>
/// Consuming code must never hardcode "Admin", "Operator", or "Viewer" — always resolve through
/// this interface so the names can be changed per-environment without source changes.
/// </remarks>
public interface ISystemRoleConfiguration
{
    /// <summary>
    /// Gets the name of the administrative role (e.g. "Admin").
    /// Never null or empty; the application fails to start if the value is missing.
    /// </summary>
    string AdminRoleName { get; }

    /// <summary>
    /// Gets the name of the operator role (e.g. "Operator"), or <c>null</c> if not configured.
    /// </summary>
    string? OperatorRoleName { get; }

    /// <summary>
    /// Gets the name of the viewer role (e.g. "Viewer"), or <c>null</c> if not configured.
    /// </summary>
    string? ViewerRoleName { get; }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="roleName"/> matches any configured system role name.
    /// Comparison is case-insensitive.
    /// </summary>
    bool IsSystemRole(string roleName);

    /// <summary>
    /// Returns <c>true</c> if <paramref name="user"/> holds the role identified by
    /// <paramref name="roleName"/>. Delegates to <see cref="ClaimsPrincipal.IsInRole"/> using
    /// the runtime-configured role name.
    /// </summary>
    bool IsInRole(ClaimsPrincipal user, string roleName);
}
