namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Minimal {name} role entry used in user-role list responses.
/// </summary>
public sealed class RoleNameEntry
{
    /// <summary>Gets or sets the role name.</summary>
    public string Name { get; set; } = string.Empty;
}
