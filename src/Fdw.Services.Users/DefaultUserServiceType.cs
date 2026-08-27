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
            // Why: Bind UsersServiceOptions from the "Users" section so CredentialServiceName is available
            // to UserCredentialService at resolution time. Missing CredentialServiceName surfaces as a
            // Critical MessageLogging failure on first credential operation, not at startup.
            builder.Services.Configure<UsersServiceOptions>(builder.Configuration.GetSection("Users"));

            // Why: Bind PasswordPolicyOptions (KDF algorithm name, password max-age, lockout threshold +
            // duration) from "Users:PasswordPolicy". The edge fails loud if a required policy input is
            // unusable (e.g. an unknown hash algorithm, or a lockout threshold with no duration).
            builder.Services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("Users:PasswordPolicy"));

            // Why: IOptionsMonitor<List<T>> is required by ImplementationConfigurationProviderBase<T,TCommand>.
            // AddOptions without BindConfiguration registers the IOptionsMonitor service so the
            // provider constructor resolves correctly. means the snapshot
            // is always empty — the gateway is the authoritative source at runtime.
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            // Why: UserConfigurationProvider is the sole owner of usr.Users gateway access. Registered
            // as a singleton so the underlying ImplementationConfigurationProviderBase cache is shared across requests.
            builder.Services.TryAddSingleton<UserConfigurationProvider>(sp =>
                new UserConfigurationProvider(
                    sp.GetService<ILogger<UserConfigurationProvider>>(),
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore, "usr"));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<UserConfiguration, UserConfigurationCommand>>(
                sp => sp.GetRequiredService<UserConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<UserConfiguration>>(
                sp => sp.GetRequiredService<UserConfigurationProvider>());

            // Why: UserTenantConfigurationProvider is the sole owner of tenant.UserTenants gateway access.
            builder.Services.TryAddSingleton<UserTenantConfigurationProvider>(sp =>
                new UserTenantConfigurationProvider(
                    sp.GetService<ILogger<UserTenantConfigurationProvider>>(),
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore, "tenant"));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<UserTenantConfiguration, UserTenantConfigurationCommand>>(
                sp => sp.GetRequiredService<UserTenantConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<UserTenantConfiguration>>(
                sp => sp.GetRequiredService<UserTenantConfigurationProvider>());

            // Why: UserPreferenceConfigurationProvider is the sole owner of usr.UserPreferences gateway
            // access. Registered as a singleton so the underlying ImplementationConfigurationProviderBase cache is
            // shared across requests.
            builder.Services.TryAddSingleton<UserPreferenceConfigurationProvider>(sp =>
                new UserPreferenceConfigurationProvider(
                    sp.GetService<ILogger<UserPreferenceConfigurationProvider>>(),
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore, "usr"));

            // Why: the credential edge hashes-on-arrival and forwards derived hashes to the password
            // credential service (the vault peppers + compares). No command façade — the verbs are the surface.
            builder.Services.TryAddScoped<IUserCredentialService, UserCredentialService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
