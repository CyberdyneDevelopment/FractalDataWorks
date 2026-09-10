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
            builder.Services.TryAddSingleton<UsersServiceConfigurationProvider>();

            builder.Services.TryAddSingleton<UserConfigurationProvider>();
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<IUserImplementationConfiguration>>(
                sp => sp.GetRequiredService<UserConfigurationProvider>());
            builder.Services.TryAddSingleton<IImplementationConfigurationProvider<IUserImplementationConfiguration>>(
                sp => sp.GetRequiredService<UserConfigurationProvider>());

            builder.Services.TryAddSingleton<UserTenantConfigurationProvider>();
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<IUserTenantImplementationConfiguration>>(
                sp => sp.GetRequiredService<UserTenantConfigurationProvider>());
            builder.Services.TryAddSingleton<IImplementationConfigurationProvider<IUserTenantImplementationConfiguration>>(
                sp => sp.GetRequiredService<UserTenantConfigurationProvider>());

            builder.Services.TryAddSingleton<UserPreferenceConfigurationProvider>();

            builder.Services.TryAddScoped<IUserCredentialService, UserCredentialService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
