using System;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;
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
    // Why: canonical name for the hub-level admin-only policy — centralised here so both
    // CalculationHub and SchemaDiscoveryHub can attribute the same string without duplication.
    internal const string SystemAdminPolicy = "system:admin";

    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FdwAuthorizationPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <param name="systemRoleConfiguration">
    /// Provides the deployment-configured admin role name so the "system:admin" policy
    /// does not embed a hardcoded role string.
    /// </param>
    public FdwAuthorizationPolicyProvider(
        IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> options,
        ISystemRoleConfiguration systemRoleConfiguration)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
        _systemRoleConfiguration = systemRoleConfiguration;
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Why: FastEndpoints generates an internal aggregate policy for each endpoint named
        // "epPolicy:{EndpointFullTypeName}" (e.g. "epPolicy:Reference.Api.Endpoints.ListUsersEndpoint").
        // This policy name contains a colon but is NOT a permission — it is an envelope that
        // FastEndpoints' own middleware fills by invoking the declared Policies("...") on the
        // endpoint. If we claim it as a permission requirement (resource="epPolicy",
        // action="...FullType..."), it maps to nothing in authz.Permission and the request 403s
        // before FDW's permission check even runs. Delegating to the fallback lets FastEndpoints
        // build the aggregate from the declared per-resource policies.
        if (policyName.StartsWith("epPolicy:", StringComparison.Ordinal))
        {
            return _fallbackProvider.GetPolicyAsync(policyName);
        }

        // Why: "system:admin" is a role-based policy that checks ISystemRoleConfiguration.AdminRoleName
        // rather than a permission claim. This allows SignalR hub methods (and any other site that
        // cannot inject ISystemRoleConfiguration via constructor) to protect admin-only operations
        // without hardcoding the role name as a string literal.
        if (string.Equals(policyName, SystemAdminPolicy, StringComparison.Ordinal))
        {
            var adminRoleName = _systemRoleConfiguration.AdminRoleName;
            var policy = new AuthorizationPolicyBuilder()
                .RequireRole(adminRoleName)
                .Build();

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

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallbackProvider.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallbackProvider.GetFallbackPolicyAsync();
}
