using System.Collections.Generic;
using Fdw.Services.Users.Clients.Models;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>Every user's role assignments, in one read.</summary>
/// <remarks>
/// Exists so a list of users can show roles without one request per row. It is deliberately the
/// same <see cref="UserRolesResponse"/> shape the per-user route returns: a screen that shows roles
/// in a list and roles on a detail page must not be able to disagree with itself about what a role
/// assignment looks like.
/// </remarks>
public sealed class AllUserRolesResponse
{
    /// <summary>Gets or sets one entry per user that holds at least one current role.</summary>
    /// <remarks>
    /// A user with no roles is absent rather than present with an empty list. The two say the same
    /// thing and only one of them can be got wrong.
    /// </remarks>
    public IReadOnlyList<UserRolesResponse> Items { get; set; } = [];
}
