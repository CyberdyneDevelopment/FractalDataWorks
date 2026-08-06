namespace Fdw.Services.Authorization;

/// <summary>
/// Options for authorization service configuration.
/// </summary>
public sealed class AuthorizationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether wildcard permissions are enabled.
    /// Default is true.
    /// </summary>
    public bool EnableWildcardPermissions { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether role hierarchy traversal is enabled.
    /// Default is true.
    /// </summary>
    public bool EnableRoleInheritance { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum depth for role hierarchy traversal to prevent infinite loops.
    /// Default is 10.
    /// </summary>
    public int MaxRoleHierarchyDepth { get; set; } = 10;
}