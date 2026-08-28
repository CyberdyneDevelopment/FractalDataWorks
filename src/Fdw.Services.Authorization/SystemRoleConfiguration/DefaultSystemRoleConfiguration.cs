using System;
using System.Security.Claims;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authorization.SystemRoleConfiguration;

/// <summary>
/// Reads system role name configuration from <see cref="SystemRoleMappingOptions"/> (bound from
/// the <c>authz:SystemRoleMapping</c> section in appsettings.json) and exposes the names as the
/// <see cref="ISystemRoleConfiguration"/> contract.
/// </summary>
/// <remarks>
/// Registration: singleton via <c>DefaultAuthorizationServiceType.Register</c>.
/// The host must call <c>services.Configure&lt;SystemRoleMappingOptions&gt;(config.GetSection("authz:SystemRoleMapping"))</c>
/// during Phase 1 (Configure) so that this class has a non-empty <see cref="AdminRoleName"/> at
/// Initialize time (after Build).
/// </remarks>
public sealed class DefaultSystemRoleConfiguration : ISystemRoleConfiguration
{
    private readonly string _adminRoleName;
    private readonly string? _operatorRoleName;
    private readonly string? _viewerRoleName;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultSystemRoleConfiguration"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="options"/> contains an empty or missing <c>AdminRoleName</c>.
    /// The app must not start without a valid admin role name — there is no fallback.
    /// </exception>
    public DefaultSystemRoleConfiguration(
        IOptions<SystemRoleMappingOptions> options,
        ILogger<DefaultSystemRoleConfiguration>? logger)
    {
        var log = logger ?? NullLogger<DefaultSystemRoleConfiguration>.Instance;
        var cfg = options.Value;

        if (cfg.AdminRoleName is null || cfg.AdminRoleName.Trim().Length == 0)
        {
            SystemRoleConfigurationLog.AdminRoleNameMissing(log);
            throw new InvalidOperationException(
                "authz:SystemRoleMapping:AdminRoleName is required and must not be empty. " +
                "Add it to appsettings.json under authz:SystemRoleMapping:AdminRoleName.");
        }

        _adminRoleName = cfg.AdminRoleName!;
        _operatorRoleName = string.IsNullOrWhiteSpace(cfg.OperatorRoleName) ? null : cfg.OperatorRoleName;
        _viewerRoleName = string.IsNullOrWhiteSpace(cfg.ViewerRoleName) ? null : cfg.ViewerRoleName;

        SystemRoleConfigurationLog.Initialized(log, _adminRoleName, _operatorRoleName, _viewerRoleName);
    }

    /// <inheritdoc />
    public string AdminRoleName => _adminRoleName;

    /// <inheritdoc />
    public string? OperatorRoleName => _operatorRoleName;

    /// <inheritdoc />
    public string? ViewerRoleName => _viewerRoleName;

    /// <inheritdoc />
    public bool IsSystemRole(string roleName)
    {
        if (string.Equals(roleName, _adminRoleName, StringComparison.OrdinalIgnoreCase))
            return true;
        if (_operatorRoleName is not null && string.Equals(roleName, _operatorRoleName, StringComparison.OrdinalIgnoreCase))
            return true;
        if (_viewerRoleName is not null && string.Equals(roleName, _viewerRoleName, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <inheritdoc />
    public bool IsInRole(ClaimsPrincipal user, string roleName)
        => user.IsInRole(roleName);
}
