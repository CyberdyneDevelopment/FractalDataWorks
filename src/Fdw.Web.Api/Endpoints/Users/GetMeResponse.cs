using System;
using System.Collections.Generic;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Response for the current authenticated user's information.
/// </summary>
public class GetMeResponse
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IList<string> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets the user's permissions.
    /// </summary>
    public IList<string> Permissions { get; set; } = [];

    /// <summary>
    /// Gets or sets the current tenant ID, if any.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the tenant IDs the user has access to.
    /// </summary>
    public IList<Guid> AvailableTenants { get; set; } = [];
}
