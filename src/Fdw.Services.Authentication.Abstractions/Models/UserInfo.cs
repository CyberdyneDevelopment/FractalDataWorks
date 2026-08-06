namespace Fdw.Services.Authentication.Clients.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Represents the current authenticated user's session information.
/// </summary>
public sealed class UserInfo
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IList<string> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets the tenant ID, if any.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the token expiry time.
    /// </summary>
    public DateTimeOffset TokenExpiry { get; set; }

    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(Username);

    /// <summary>
    /// Returns <c>true</c> if the user holds the administrative role as defined by
    /// <paramref name="systemRoleConfiguration"/>.
    /// </summary>
    /// <param name="systemRoleConfiguration">
    /// The deployment-level system role configuration supplying the configured admin role name.
    /// </param>
    /// <remarks>
    /// Why: the admin role name is deployment-configurable via <c>authz:SystemRoleMapping:AdminRoleName</c>;
    /// hardcoding "Admin" here would break environments that use a different role name.
    /// </remarks>
    public bool IsInAdminRole(ISystemRoleConfiguration systemRoleConfiguration)
        => Roles.Any(r => string.Equals(r, systemRoleConfiguration.AdminRoleName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a value indicating whether the token is expiring within 60 seconds.
    /// </summary>
    public bool IsTokenExpiring => TokenExpiry <= DateTimeOffset.UtcNow.AddSeconds(60);

    /// <summary>
    /// Converts the user info to a collection of claims for use in a claims identity.
    /// </summary>
    /// <returns>An enumerable of claims representing this user.</returns>
    public IEnumerable<Claim> ToClaims()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Username),
            new Claim(ClaimTypes.NameIdentifier, UserId),
        };

        foreach (var role in Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (TenantId.HasValue)
        {
            claims.Add(new Claim(ClaimDefinitions.tenantId.Name, TenantId.Value.ToString()));
        }

        return claims;
    }
}
