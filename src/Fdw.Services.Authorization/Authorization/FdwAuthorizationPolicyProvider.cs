using System;
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
/// "{resource}:{action}" policy names. Falls back to the default
/// policy provider for non-matching policies.
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

        // "{resource}:{action}" shape — claim it as a permission policy.
        var parts = policyName.Split(':', 2);
        if (parts.Length == 2
            && !string.IsNullOrEmpty(parts[0])
            && !string.IsNullOrEmpty(parts[1]))
        {
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new FdwPermissionRequirement(parts[0], parts[1]))
                .Build();

            FrameworkAuthorizationLog.PolicyResolvedToPermission(_logger, policyName, parts[0], parts[1]);
            return Task.FromResult<AuthorizationPolicy?>(policy);
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
