using System;
using System.Collections.Generic;
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
    /// Initializes a new instance of the <see cref="FdwPermissionRequirement"/> class that any one of several permissions satisfies (FDW-2246).
    /// </summary>
    /// <param name="resource">The resource of the first accepted permission.</param>
    /// <param name="action">The action of the first accepted permission.</param>
    /// <param name="alsoAccepted">The other permissions that satisfy the requirement, as resource and action; not empty.</param>
    public FdwPermissionRequirement(string resource, string action, IReadOnlyList<(string Resource, string Action)> alsoAccepted)
        : this(resource, action)
    {
        ArgumentNullException.ThrowIfNull(alsoAccepted);
        if (alsoAccepted.Count == 0)
            throw new ArgumentException("A requirement with alternatives names at least one more permission.", nameof(alsoAccepted));

        AlsoAccepted = alsoAccepted;
    }

    /// <summary>
    /// Gets the other permissions that satisfy this requirement as well as <see cref="Resource"/>:<see cref="Action"/>; empty when only that one does.
    /// </summary>
    public IReadOnlyList<(string Resource, string Action)> AlsoAccepted { get; } = [];

    /// <summary>
    /// Gets the resource being accessed.
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// Gets the action being performed.
    /// </summary>
    public string Action { get; }
}
