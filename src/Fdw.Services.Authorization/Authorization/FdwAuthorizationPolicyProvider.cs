using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authorization.Authorization;

/// <summary>
/// Dynamic policy provider that creates authorization policies for
/// "{resource}:{action}" policy names, and for "{resource}:{action}|{resource}:{action}" names that any one of the
/// permissions satisfies. Falls back to the default policy provider for non-matching policies.
/// </summary>
/// <remarks>
/// <para>
/// Policy names follow the bare "{resource}:{action}" pattern, e.g.:
/// <list type="bullet">
/// <item><description>"connections:read" → requires connections:read permission</description></item>
/// <item><description>"datastores:write" → requires datastores:write permission</description></item>
/// <item><description>"pipelines:execute" → requires pipelines:execute permission</description></item>
/// </list>
/// </para>
/// <para>
/// The previous "fdw:" prefix was removed because it baked a framework brand into the
/// authorization surface. Per-tenant API branding is applied at the DTO boundary via the
/// tenant's <see cref="Fdw.Services.Multitenancy.Abstractions.ITenant.OrgPrefix"/>,
/// not in the policy provider — the provider sees bare "{resource}:{action}" names that
/// endpoints declare in <c>Policies(...)</c> and that storage holds in authz.Permission.
/// </para>
/// <para>
/// This provider works with <see cref="FrameworkPermissionHandler"/> which delegates
/// the actual permission check to the FDW <see cref="Fdw.Services.Authentication.Abstractions.Security.IFrameworkAuthorizationService"/>.
/// </para>
/// </remarks>
public sealed class FdwAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    internal const string SystemAdminPolicy = AuthConstants.PlatformAdminPolicy;

    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;
    private readonly ILogger<FdwAuthorizationPolicyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FdwAuthorizationPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <param name="systemRoleConfiguration">
    /// Provides the deployment-configured admin role name so the "system:admin" policy
    /// does not embed a hardcoded role string.
    /// </param>
    /// <param name="logger">The logger.</param>
    public FdwAuthorizationPolicyProvider(
        IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> options,
        ISystemRoleConfiguration systemRoleConfiguration,
        ILogger<FdwAuthorizationPolicyProvider>? logger = null)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
        _systemRoleConfiguration = systemRoleConfiguration;
        _logger = logger ?? NullLogger<FdwAuthorizationPolicyProvider>.Instance;
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("epPolicy:", StringComparison.Ordinal))
        {
            return _fallbackProvider.GetPolicyAsync(policyName);
        }

        if (string.Equals(policyName, SystemAdminPolicy, StringComparison.Ordinal))
        {
            // The platform's administrator, not any tenant's: the admin role held while acting in the
            // global tenant (FDW-825). A tenant Admin carries the same role name in a token for their
            // own tenant, which is why the role alone cannot answer this.
            var adminRoleName = _systemRoleConfiguration.AdminRoleName;
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(context => _systemRoleConfiguration.IsPlatformAdmin(context.User))
                .Build();

            FrameworkAuthorizationLog.PolicyResolvedToAdminRole(_logger, policyName, adminRoleName);
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Bare "authenticated" — requires a logged-in user, no specific permission.
        if (string.Equals(policyName, "authenticated", StringComparison.Ordinal))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // "{resource}:{action}" shape, or several joined by a bar when any one is enough (FDW-2246): claim it as a permission policy. The name is read by
        // PermissionPolicyName, the rule the pre-processor reads it by too.
        if (PermissionPolicyName.TryParse(policyName, out var permissions))
        {
            var (resource, action) = permissions[0];
            var requirement = permissions.Count == 1
                ? new FdwPermissionRequirement(resource, action)
                : new FdwPermissionRequirement(resource, action, permissions.Skip(1).ToList());

            FrameworkAuthorizationLog.PolicyResolvedToPermission(_logger, policyName, resource, action);
            return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder().AddRequirements(requirement).Build());
        }

        FrameworkAuthorizationLog.PolicyFellThroughToDefault(_logger, policyName);
        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallbackProvider.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallbackProvider.GetFallbackPolicyAsync();
}
