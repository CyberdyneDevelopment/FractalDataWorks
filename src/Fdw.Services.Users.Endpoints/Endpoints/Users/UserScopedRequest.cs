namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Base request for any route scoped to a single user by its <c>{IdOrName}</c> segment (API-66).
/// The segment is bound as a string so callers may pass either a Guid id or a username; endpoints
/// resolve it through <c>UserConfigurationProvider.ResolveUser</c>.
/// </summary>
public class UserScopedRequest
{
    /// <summary>
    /// Gets or sets the user's Guid id or username, bound from the route.
    /// </summary>
    public string IdOrName { get; set; } = string.Empty;
}
