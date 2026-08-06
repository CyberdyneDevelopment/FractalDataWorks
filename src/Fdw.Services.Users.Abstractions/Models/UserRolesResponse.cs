using System;
using System.Collections.Generic;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// Represents the roles assigned to a specific user.
/// </summary>
public sealed class UserRolesResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the list of role names assigned to the user.
    /// </summary>
    // Why IList rather than IReadOnlyList: this type is now the single declaration used by the
    // server endpoint too, and FastEndpoints needs a mutable collection to bind incoming JSON.
    public IList<string> Roles { get; set; } = [];
}
