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
[Implementation(typeof(AuthorizationServiceTypes), "Default")]
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


            builder.Services.TryAddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
            builder.Services.TryAddSingleton<IEffectivePermissionResolver>(sp =>
                new EffectivePermissionResolver(
                    sp.GetRequiredService<IRoleConfigurationProvider>(),
                    sp.GetRequiredService<IPermissionConfigurationProvider>(),
                    sp.GetRequiredService<IRolePermissionConfigurationProvider>(),
                    sp.GetRequiredService<IUserRoleConfigurationProvider>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    sp.GetService<ILoggerFactory>()?.CreateLogger<EffectivePermissionResolver>(),
                    sp.GetRequiredService<IOrgAccessProvider>()));

            builder.Services.TryAddSingleton<IRolePermissionResolver>(sp =>
                new RolePermissionResolver(
                    sp.GetRequiredService<IRoleConfigurationProvider>(),
                    sp.GetRequiredService<IPermissionConfigurationProvider>(),
                    sp.GetRequiredService<IRolePermissionConfigurationProvider>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    sp.GetService<ILoggerFactory>()?.CreateLogger<RolePermissionResolver>()));

            builder.Services.TryAddSingleton<IFrameworkAuthorizationService, DefaultAuthorizationService>();

            builder.Services.AddSingleton<IAspNetAuthorizationPolicyProvider, FdwAuthorizationPolicyProvider>();
            builder.Services.AddSingleton<IAspNetAuthorizationHandler, FrameworkPermissionHandler>();

            builder.Services.AddAuthorization();


            builder.Services.AddSingleton<IRoleImplementationConfigurationProvider, RoleImplementationConfigurationProvider>(sp => new RoleImplementationConfigurationProvider(sp.GetRequiredService<ILogger<RoleImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection));
            // Why also the generic form: DefaultAuthorizationService asks for
            // IImplementationConfigurationProvider<T>, and DI registers exactly the service type it is
            // given -- a named interface that EXTENDS the generic one does not satisfy a request for the
            // generic one. Registering only the named form left the service unresolvable, which surfaced
            // three domains later as "Unable to resolve service for type ... while attempting to activate
            // DefaultAuthorizationService".
            builder.Services.TryAddSingleton<IImplementationConfigurationProvider<IRoleImplementationConfiguration>>(
                sp => sp.GetRequiredService<IRoleImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<RoleConfigurationProvider>(sp =>
            {
                var domain = new RoleConfigurationProvider(
                    sp.GetRequiredService<ILogger<RoleConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection);
                domain.Register("Role", sp.GetRequiredService<IRoleImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IRoleConfigurationProvider>(sp => sp.GetRequiredService<RoleConfigurationProvider>());



            builder.Services.AddSingleton<IUserRoleImplementationConfigurationProvider, UserRoleImplementationConfigurationProvider>(sp => new UserRoleImplementationConfigurationProvider(sp.GetRequiredService<ILogger<UserRoleImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<UserRoleConfigurationProvider>(sp =>
            {
                var domain = new UserRoleConfigurationProvider(
                    sp.GetRequiredService<ILogger<UserRoleConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection);
                domain.Register("UserRole", sp.GetRequiredService<IUserRoleImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IUserRoleConfigurationProvider>(sp => sp.GetRequiredService<UserRoleConfigurationProvider>());


            builder.Services.AddSingleton<IPermissionImplementationConfigurationProvider, PermissionImplementationConfigurationProvider>(sp => new PermissionImplementationConfigurationProvider(sp.GetRequiredService<ILogger<PermissionImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<IImplementationConfigurationProvider<IPermissionImplementationConfiguration>>(
                sp => sp.GetRequiredService<IPermissionImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<PermissionConfigurationProvider>(sp =>
            {
                var domain = new PermissionConfigurationProvider(
                    sp.GetRequiredService<ILogger<PermissionConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection);
                domain.Register("Permission", sp.GetRequiredService<IPermissionImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IPermissionConfigurationProvider>(sp => sp.GetRequiredService<PermissionConfigurationProvider>());


            builder.Services.AddSingleton<IRolePermissionImplementationConfigurationProvider, RolePermissionImplementationConfigurationProvider>(sp => new RolePermissionImplementationConfigurationProvider(sp.GetRequiredService<ILogger<RolePermissionImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<IImplementationConfigurationProvider<IRolePermissionImplementationConfiguration>>(
                sp => sp.GetRequiredService<IRolePermissionImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<RolePermissionConfigurationProvider>(sp =>
            {
                var domain = new RolePermissionConfigurationProvider(
                    sp.GetRequiredService<ILogger<RolePermissionConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection);
                domain.Register("RolePermission", sp.GetRequiredService<IRolePermissionImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IRolePermissionConfigurationProvider>(sp => sp.GetRequiredService<RolePermissionConfigurationProvider>());


            // Hands over the gateway provider, not a gateway. Resolving one here meant .Value! on a
            // result that can fail — a null-forgive that turns "no gateway for this connection" into
            // a NullReferenceException at the first query instead of a named failure at the read.
            builder.Services.TryAddSingleton<TenantOrgAccessConfigurationProvider>(sp =>
                new TenantOrgAccessConfigurationProvider(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    sp.GetService<ILogger<TenantOrgAccessConfigurationProvider>>()));

            builder.Services.TryAddSingleton<IOrgAccessProvider>(sp =>
                new DefaultOrgAccessProvider(
                    sp.GetRequiredService<TenantOrgAccessConfigurationProvider>(),
                    sp.GetService<ILogger<DefaultOrgAccessProvider>>()));

            builder.Services.AddSingleton<ISystemRoleMappingConfigurationProvider, SystemRoleMappingConfigurationProvider>(sp => new SystemRoleMappingConfigurationProvider(sp.GetRequiredService<ILogger<SystemRoleMappingConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection));

            // Why the domain provider and not the implementation one: the domain row names which
            // mapping this host runs, and routing to it is the domain provider's job. Reading the
            // implementation directly would name one in code and make the record decorative.
            builder.Services.TryAddSingleton<IRoleMappingConfigurationProvider>(sp =>
            {
                var domain = new RoleMappingConfigurationProvider(
                    sp.GetRequiredService<ILogger<RoleMappingConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthorizationServiceTypes.ConfigurationConnection);
                domain.Register("System", sp.GetRequiredService<ISystemRoleMappingConfigurationProvider>());
                return domain;
            });

            // Why the row is read here rather than injected: DefaultSystemRoleConfiguration throws
            // when no administrator role is named, and it does that at construction so the process
            // fails at startup instead of at the first authorization check.
            builder.Services.TryAddSingleton<ISystemRoleConfiguration>(sp =>
                new DefaultSystemRoleConfiguration(
                    ReadSystemRoleMapping(sp),
                    sp.GetService<ILogger<DefaultSystemRoleConfiguration>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

        Configuration(builder =>
        {

            builder.Services.AddOptions<List<RoleImplementationConfiguration>>()
                .BindConfiguration("Roles");
            builder.Services.AddOptions<List<PermissionImplementationConfiguration>>()
                .BindConfiguration("Permissions");
            builder.Services.AddOptions<List<UserRoleImplementationConfiguration>>()
                .BindConfiguration("UserRoles");
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

    }

    // Why blocking: this runs inside a DI factory, which cannot await, and the value is needed to
    // decide whether the process may start at all.
    private static SystemRoleMappingConfiguration ReadSystemRoleMapping(IServiceProvider services)
    {
        var provider = services.GetRequiredService<IRoleMappingConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get("SystemRoleMapping").GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is null)
        {
            throw new InvalidOperationException(
                "SystemRoleMapping is not configured in the authorization store. " +
                "Authorization cannot start without knowing which role names carry system authority.");
        }

        // The domain provider returns the implementation its row named. Anything other than the
        // System mapping here means the row names an implementation this host does not run, which is
        // a configuration fault rather than something to coerce.
        if (result.Value is not SystemRoleMappingConfiguration system)
        {
            throw new InvalidOperationException(
                $"The RoleMapping row named implementation '{result.Value.Name}', "
                + "which is not the System role mapping. Authorization cannot start without knowing "
                + "which role names carry system authority.");
        }

        return system;
    }
}
