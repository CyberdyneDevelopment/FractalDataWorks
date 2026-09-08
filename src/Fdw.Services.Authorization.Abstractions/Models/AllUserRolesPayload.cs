using System;
using System.Collections.Generic;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Every user's role assignments, in one read.
/// </summary>
/// <remarks>
/// Carries <see cref="UserRolesPayload"/> rather than a shape of its own, so a screen showing roles
/// in a list and roles on a detail page cannot disagree with itself about what an assignment looks
/// like. A user holding no roles is absent rather than present with an empty list.
/// </remarks>
public sealed class AllUserRolesPayload
{
    /// <summary>Gets or sets one entry per user that holds at least one current role.</summary>
    public IReadOnlyList<UserRolesPayload> Items { get; set; } = Array.Empty<UserRolesPayload>();
}
