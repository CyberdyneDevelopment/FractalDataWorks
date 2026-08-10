namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Base request for any route scoped to a single user by its <c>{IdOrName}</c> segment (API-66).
/// The segment is bound as a string so callers may pass either a Guid id or a username; endpoints
/// resolve it through <c>UserConfigurationProvider.ResolveUser</c>.
/// </summary>
// Why: every user-scoped route binds the SAME property name so the resolution contract is declared
// once. The user-role endpoints previously each declared their own segment — one a Guid 'UserId',
// two a username-only 'Name' — and clients that sent an id could add a role but never revoke it.
// Inheriting this base is what makes that divergence impossible to reintroduce.
public class UserScopedRequest
{
    /// <summary>
    /// Gets or sets the user's Guid id or username, bound from the route.
    /// </summary>
    public string IdOrName { get; set; } = string.Empty;
}
