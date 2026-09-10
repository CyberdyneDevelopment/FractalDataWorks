using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Models;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Fdw.Results;

namespace Fdw.Services.Users;

/// <summary>
/// Default user service type that registers user stores and credential validation
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(UserServiceTypes), "Default")]
public sealed class DefaultUserServiceType : UserServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserServiceType"/> class.
    /// </summary>
    public DefaultUserServiceType()
        : base(
            "Default",
            "Users:Default",
            "Default User Services",
            "Default user management services with credential validation and role-based access control")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<UsersServiceImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IUsersServiceImplementationConfigurationProvider>(sp => sp.GetRequiredService<UsersServiceImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<UsersServiceConfigurationProvider>(sp =>
            {
                var domain = new UsersServiceConfigurationProvider(
                    sp.GetRequiredService<ILogger<UsersServiceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("UsersService", sp.GetRequiredService<IUsersServiceImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IUsersServiceConfigurationProvider>(sp => sp.GetRequiredService<UsersServiceConfigurationProvider>());


            builder.Services.TryAddSingleton<UserImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IUserImplementationConfigurationProvider>(sp => sp.GetRequiredService<UserImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<UserConfigurationProvider>(sp =>
            {
                var domain = new UserConfigurationProvider(
                    sp.GetRequiredService<ILogger<UserConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("Users", sp.GetRequiredService<IUserImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IUserConfigurationProvider>(sp => sp.GetRequiredService<UserConfigurationProvider>());


            builder.Services.TryAddSingleton<UserTenantImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IUserTenantImplementationConfigurationProvider>(sp => sp.GetRequiredService<UserTenantImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<UserTenantConfigurationProvider>(sp =>
            {
                var domain = new UserTenantConfigurationProvider(
                    sp.GetRequiredService<ILogger<UserTenantConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("UserTenants", sp.GetRequiredService<IUserTenantImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IUserTenantConfigurationProvider>(sp => sp.GetRequiredService<UserTenantConfigurationProvider>());


            builder.Services.TryAddSingleton<UserPreferencesImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IUserPreferencesImplementationConfigurationProvider>(sp => sp.GetRequiredService<UserPreferencesImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<UserPreferenceConfigurationProvider>(sp =>
            {
                var domain = new UserPreferenceConfigurationProvider(
                    sp.GetRequiredService<ILogger<UserPreferenceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("UserPreferences", sp.GetRequiredService<IUserPreferencesImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IUserPreferenceConfigurationProvider>(sp => sp.GetRequiredService<UserPreferenceConfigurationProvider>());


            builder.Services.TryAddScoped<IUserCredentialService, UserCredentialService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
