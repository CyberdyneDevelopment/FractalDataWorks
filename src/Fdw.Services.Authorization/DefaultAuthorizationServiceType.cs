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
[ServiceTypeOption(typeof(AuthorizationServiceTypes), "Default")]
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


            builder.Services.TryAddSingleton<IEffectivePermissionResolver>(sp =>
                new EffectivePermissionResolver(
                    sp.GetRequiredService<IServiceConfigurationProvider<RoleConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<PermissionConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<RolePermissionConfiguration>>(),
                    sp.GetRequiredService<UserRoleConfigurationProvider>(),
                    sp.GetService<ILoggerFactory>()?.CreateLogger<EffectivePermissionResolver>(),
                    new Lazy<IOrgAccessProvider>(() => sp.GetRequiredService<IOrgAccessProvider>())));

            builder.Services.TryAddSingleton<IRolePermissionResolver>(sp =>
                new RolePermissionResolver(
                    sp.GetRequiredService<IServiceConfigurationProvider<RoleConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<PermissionConfiguration>>(),
                    sp.GetRequiredService<IServiceConfigurationProvider<RolePermissionConfiguration>>(),
                    sp.GetService<ILoggerFactory>()?.CreateLogger<RolePermissionResolver>()));

            builder.Services.TryAddSingleton<IFrameworkAuthorizationService, DefaultAuthorizationService>();

            builder.Services.AddSingleton<IAspNetAuthorizationPolicyProvider, FdwAuthorizationPolicyProvider>();
            builder.Services.AddSingleton<IAspNetAuthorizationHandler, FrameworkPermissionHandler>();

            builder.Services.AddAuthorization();

            const string pathNameAuthz = "authz";

            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStore, pathNameAuthz));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<PermissionConfiguration>>(sp =>
                sp.GetRequiredService<ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>>());

            // RolePermission junction provider.
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStore, pathNameAuthz));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<RolePermissionConfiguration>>(sp =>
                sp.GetRequiredService<ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>>());

            builder.Services.TryAddSingleton<TenantOrgAccessConfigurationProvider>(sp =>
                new TenantOrgAccessConfigurationProvider(
                    sp.GetRequiredService<IConfigurationGatewayProvider>()
                        .Get(AuthorizationServiceTypes.ConfigurationConnection).Value!,
                    sp.GetService<ILogger<TenantOrgAccessConfigurationProvider>>()));

            builder.Services.TryAddScoped<IOrgAccessProvider>(sp =>
                new DefaultOrgAccessProvider(
                    sp.GetRequiredService<TenantOrgAccessConfigurationProvider>(),
                    sp.GetService<ILogger<DefaultOrgAccessProvider>>()));

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
            builder.Services.AddOptions<SystemRoleMappingOptions>()
                .BindConfiguration("authz:SystemRoleMapping");
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

    }

}
