using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Conventions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Authorization;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.SystemRoleConfiguration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IAspNetAuthorizationHandler = Microsoft.AspNetCore.Authorization.IAuthorizationHandler;
using IAspNetAuthorizationPolicyProvider = Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider;
using Fdw.Results;

namespace Fdw.Services.Authorization;

/// <summary>
/// Default authorization service type that registers <see cref="IFrameworkAuthorizationService"/>
/// and ASP.NET Core authorization bridge components with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AuthorizationTypes), "Default")]
public sealed class DefaultAuthorizationServiceType : AuthorizationTypeBase<IGenericService, IAuthorizationFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAuthorizationServiceType"/> class.
    /// </summary>
#pragma warning disable MA0051
#pragma warning disable FDW006 // sequential DI registration for authorization infrastructure
    [ConventionOverride(MaxMethodLines = 80)]  // Sequential DI registration for authorization infrastructure — one statement per service.
    public DefaultAuthorizationServiceType()
        : base(
            "Default",
            "Authorization:Default",
            "Default Authorization",
            "Default authorization service with ASP.NET Core policy bridge and database-backed permissions",
            defaultContainerName: "Role")
    {
        Registration((builder, loggerFactory) =>
        {

            // Why: UserRoleConfigurationProvider must be registered before EffectivePermissionResolver
            // because FDW-532 fix requires the resolver to load user role assignments first.
            // The registration block below is intentionally placed before EffectivePermissionResolver
            // even though DI resolves lazily — this makes the dependency chain explicit.
            // (The actual UserRoleConfigurationProvider registration is later in this method; DI
            // resolves it lazily so ordering doesn't matter at runtime, but the comment is here for
            // clarity about why the resolver now requires UserRoleConfigurationProvider.)

            // Why: EffectivePermissionResolver is registered first so it can be resolved by both
            // DefaultAuthorizationService (per-request path) and JwtAuthenticationService (token-issue path).
            // Singleton is safe: it holds only providers (also singletons) and Lazy<IOrgAccessProvider>.
            // FDW-532: UserRoleConfigurationProvider is now required to scope permission resolution
            // to the user's actual role assignments — prevents baking the full catalog into every token.
            builder.Services.TryAddSingleton<IEffectivePermissionResolver>(sp =>
                new EffectivePermissionResolver(
                    sp.GetRequiredService<IServiceConfigurationProvider<RoleConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<PermissionConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<RolePermissionConfiguration>>(),
                    sp.GetRequiredService<UserRoleConfigurationProvider>(),
                    sp.GetService<ILoggerFactory>()?.CreateLogger<EffectivePermissionResolver>(),
                    // Why: Lazy<IOrgAccessProvider> is injected here so the resolver can pick up the
                    // org-access provider once it has been registered by the calling code (which may
                    // register it after this point in the registration chain).
                    new Lazy<IOrgAccessProvider>(() => sp.GetRequiredService<IOrgAccessProvider>())));

            // Why singleton alongside the user-keyed resolver rather than inside it: this answers
            // "what does holding these roles grant", which has no user in it. An authentication service
            // that establishes a principal from an external issuer resolves its declared roles through
            // this, and lands on the same authz.RolePermission expansion the user path uses.
            builder.Services.TryAddSingleton<IRolePermissionResolver>(sp =>
                new RolePermissionResolver(
                    sp.GetRequiredService<IServiceConfigurationProvider<RoleConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<PermissionConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<RolePermissionConfiguration>>(),
                    sp.GetService<ILoggerFactory>()?.CreateLogger<RolePermissionResolver>()));

            // Why: DefaultAuthorizationService reads from dual-source providers (system ctrl + user cfg).
            builder.Services.TryAddSingleton<IFrameworkAuthorizationService, DefaultAuthorizationService>();

            // Why: ASP.NET Core bridge — converts Policies("resource:action") to provider-based checks.
            builder.Services.AddSingleton<IAspNetAuthorizationPolicyProvider, FdwAuthorizationPolicyProvider>();
            builder.Services.AddSingleton<IAspNetAuthorizationHandler, FrameworkPermissionHandler>();

            // Why: the bridge above plugs INTO ASP.NET Core's authorization system — AddAuthorization()
            // registers that system's own builder.Services (IAuthorizationService, policy/handler providers,
            // the builder.Services UseAuthorization() depends on). Microsoft infrastructure required by this
            // option's builder.Services belongs HERE (like AddHttpClient on HTTP options), never as a separate
            // Program.cs line. Safe if the host also calls it: the only repeat effect is a duplicate
            // no-op IConfigureOptions<AuthorizationOptions>; IAuthorizationHandler is TryAddEnumerable.
            builder.Services.AddAuthorization();

            const string pathNameAuthz = "authz";

            // Why: RoleConfigurationProvider exposes IAuthorizationProvider (GetPermissions etc.).
            // Registered via the domain cascade so every consumer option shares ONE canonical
            // registration shape instead of racing TryAdds.
            RoleConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            // Permission dual-source provider (no custom subclass — plain inline provider).
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore, pathNameAuthz));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<PermissionConfiguration>>(sp =>
                sp.GetRequiredService<ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>>());

            // RolePermission junction provider.
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore, pathNameAuthz));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<RolePermissionConfiguration>>(sp =>
                sp.GetRequiredService<ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>>());

            // Why: TenantOrgAccessConfigurationProvider is the domain-owned gateway path for TenantOrgAccess.
            // Registered as singleton — it holds no per-request state.
            builder.Services.TryAddSingleton<TenantOrgAccessConfigurationProvider>(sp =>
                new TenantOrgAccessConfigurationProvider(
                    sp.GetRequiredService<IConfigurationGateway>(),
                    sp.GetService<ILogger<TenantOrgAccessConfigurationProvider>>()));

            // Why: IOrgAccessProvider reads org-tier grants via TenantOrgAccessConfigurationProvider
            // (ConfigurationDb). Scoped because EffectivePermissionResolver resolves it via
            // Lazy<IOrgAccessProvider> at first call within the request.
            builder.Services.TryAddScoped<IOrgAccessProvider>(sp =>
                new DefaultOrgAccessProvider(
                    sp.GetRequiredService<TenantOrgAccessConfigurationProvider>(),
                    sp.GetService<ILogger<DefaultOrgAccessProvider>>()));

            // Why: UserRoleConfigurationProvider handles user-role assignments with GetByUser() filtering.
            UserRoleConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            // Why: ISystemRoleConfiguration is the single source of truth for all system role name checks
            // throughout the framework (SetRolePermissionsEndpointBase, RequestTenantInfo, hub policies, etc.).
            // Registered here so every consumer that injects ISystemRoleConfiguration receives the same
            // singleton instance backed by appsettings authz:SystemRoleMapping.
            builder.Services.TryAddSingleton<ISystemRoleConfiguration>(sp =>
                new DefaultSystemRoleConfiguration(
                    sp.GetRequiredService<IOptions<SystemRoleMappingOptions>>(),
                    sp.GetService<ILogger<DefaultSystemRoleConfiguration>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

        Configuration(builder =>
        {

            builder.Services.AddOptions<List<RoleConfiguration>>()
                .BindConfiguration("Roles");
            builder.Services.AddOptions<List<PermissionConfiguration>>()
                .BindConfiguration("Permissions");
            builder.Services.AddOptions<List<UserRoleConfiguration>>()
                .BindConfiguration("UserRoles");
            // Why: authz:SystemRoleMapping is the section that backs ISystemRoleConfiguration.
            // Binding here keeps all authorization builder.Configuration registration in one place.
            builder.Services.AddOptions<SystemRoleMappingOptions>()
                .BindConfiguration("authz:SystemRoleMapping");
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

    }

}
