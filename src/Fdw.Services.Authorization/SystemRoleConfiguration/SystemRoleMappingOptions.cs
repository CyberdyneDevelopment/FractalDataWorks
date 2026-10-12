namespace Fdw.Services.Authorization.SystemRoleConfiguration;

/// <summary>
/// POCO bound from the <c>authz:SystemRoleMapping</c> section in appsettings.json.
/// Used as the options payload for <see cref="DefaultSystemRoleConfiguration"/>.
/// </summary>
/// <remarks>
/// All properties use <c>{ get; set; }</c> so IOptions binding works correctly.
/// No default values — a missing or empty <c>AdminRoleName</c> is a startup error detected
/// inside <see cref="DefaultSystemRoleConfiguration"/>.
/// </remarks>
public sealed class SystemRoleMappingOptions
{
    /// <summary>
    /// Gets or sets the admin role name. Required — the application fails to start if absent or empty.
    /// </summary>
    public string? AdminRoleName { get; set; }

    /// <summary>
    /// Gets or sets the operator role name, or <c>null</c> if not used.
    /// </summary>
    public string? OperatorRoleName { get; set; }

    /// <summary>
    /// Gets or sets the viewer role name, or <c>null</c> if not used.
    /// </summary>
    public string? ViewerRoleName { get; set; }
}
