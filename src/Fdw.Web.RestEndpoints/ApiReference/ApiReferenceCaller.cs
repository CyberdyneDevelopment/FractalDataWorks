using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// Who an API reference document is being cut for: signed in or not, platform admin or not, and the
/// permissions the caller holds.
/// </summary>
/// <remarks>
/// Read once per request from the caller's own claims, the same claims the endpoints themselves
/// authorize against, so the reference shows exactly the operations the caller could call.
/// </remarks>
public sealed class ApiReferenceCaller
{
    private ApiReferenceCaller(bool isAuthenticated, bool isPlatformAdmin, IReadOnlySet<string> permissions)
    {
        IsAuthenticated = isAuthenticated;
        IsPlatformAdmin = isPlatformAdmin;
        Permissions = permissions;
    }

    /// <summary>Gets a value indicating whether the caller is signed in.</summary>
    public bool IsAuthenticated { get; }

    /// <summary>Gets a value indicating whether the caller is a platform admin, who may call every operation.</summary>
    public bool IsPlatformAdmin { get; }

    /// <summary>Gets the permissions the caller holds.</summary>
    public IReadOnlySet<string> Permissions { get; }

    /// <summary>Gets how the caller is described in the log.</summary>
    public string Kind => !IsAuthenticated ? "anonymous" : IsPlatformAdmin ? "platform admin" : "signed-in";

    /// <summary>Reads the caller from their claims.</summary>
    /// <param name="user">The request's user.</param>
    /// <param name="systemRoles">Decides whether the user is a platform admin.</param>
    /// <returns>The caller.</returns>
    public static ApiReferenceCaller From(ClaimsPrincipal user, ISystemRoleConfiguration systemRoles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(systemRoles);

        if (user.Identity?.IsAuthenticated != true)
            return new ApiReferenceCaller(false, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        return new ApiReferenceCaller(
            true,
            systemRoles.IsPlatformAdmin(user),
            user.FindAll(ClaimDefinitions.perm.Name)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Decides whether the caller may call an operation.</summary>
    /// <param name="isAnonymous">Whether the operation admits anonymous callers.</param>
    /// <param name="policies">The policies the operation requires.</param>
    /// <returns>Why the caller may not call it, or <see langword="null"/> when they may.</returns>
    public string? Refusal(bool isAnonymous, IReadOnlyList<string> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        if (isAnonymous)
            return null;

        if (!IsAuthenticated)
            return "it requires a signed-in caller";

        if (IsPlatformAdmin)
            return null;

        return policies.FirstOrDefault(policy => !Permissions.Contains(policy)) is { } missing
            ? "the caller does not hold '" + missing + "'"
            : null;
    }
}
