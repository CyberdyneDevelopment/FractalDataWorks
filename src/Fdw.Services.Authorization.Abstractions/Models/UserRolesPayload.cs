using System;
using System.Collections.Generic;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Data transfer object for user roles.
/// </summary>
public sealed class UserRolesPayload
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
