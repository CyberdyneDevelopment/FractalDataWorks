using System;
using Microsoft.AspNetCore.Authorization;

namespace Fdw.Services.Authorization.Authorization;

/// <summary>
/// ASP.NET Core authorization requirement that maps to an FDW permission check.
/// Represents a required permission in the format "resource:action".
/// </summary>
public sealed class FdwPermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FdwPermissionRequirement"/> class.
    /// </summary>
    /// <param name="resource">The resource being accessed (e.g., "connections", "datastores").</param>
    /// <param name="action">The action being performed (e.g., "read", "write", "delete").</param>
    public FdwPermissionRequirement(string resource, string action)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Gets the resource being accessed.
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// Gets the action being performed.
    /// </summary>
    public string Action { get; }
}
