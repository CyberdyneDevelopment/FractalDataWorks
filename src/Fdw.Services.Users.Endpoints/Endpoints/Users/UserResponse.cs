using System;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Response model for user information (excludes sensitive data).
/// </summary>
public class UserResponse : ResourceDetail
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets whether the user is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IList<string> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets when the user was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a friendly display name for the user. Defaults to Username when not set.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets when the user last logged in.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the principal that created the user record (audit trail).
    /// </summary>
    public string? CreatedBy { get; set; }
}
