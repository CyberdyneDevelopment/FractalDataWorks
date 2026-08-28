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
[ServiceTypeOption(typeof(UserServiceTypes), "Default")]
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
        Configuration(builder =>
        {
            builder.Services.Configure<UsersServiceOptions>(builder.Configuration.GetSection("Users"));

            builder.Services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("Users:PasswordPolicy"));

    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<UserConfigurationProvider>(sp =>
                new UserConfigurationProvider(
                    sp.GetService<ILogger<UserConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStore, "usr"));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<UserConfiguration, UserConfigurationCommand>>(
                sp => sp.GetRequiredService<UserConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<UserConfiguration>>(
                sp => sp.GetRequiredService<UserConfigurationProvider>());

            builder.Services.TryAddSingleton<UserTenantConfigurationProvider>(sp =>
                new UserTenantConfigurationProvider(
                    sp.GetService<ILogger<UserTenantConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStore, "tenant"));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<UserTenantConfiguration, UserTenantConfigurationCommand>>(
                sp => sp.GetRequiredService<UserTenantConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<UserTenantConfiguration>>(
                sp => sp.GetRequiredService<UserTenantConfigurationProvider>());

            builder.Services.TryAddSingleton<UserPreferenceConfigurationProvider>(sp =>
                new UserPreferenceConfigurationProvider(
                    sp.GetService<ILogger<UserPreferenceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStore, "usr"));

            builder.Services.TryAddScoped<IUserCredentialService, UserCredentialService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
