using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Commands.Data;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings.Commands;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Settings;

/// <summary>
/// Default settings service type. Registers the gateway-backed SettingsConfigurationProvider
/// (server/tenant/role settings) and the layered IEffectiveSettingsProvider.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(SettingsServiceTypes), "Default")]
public sealed class DefaultSettingsServiceType : SettingsServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSettingsServiceType"/> class.
    /// </summary>
    public DefaultSettingsServiceType()
        : base(
            "Default",
            "Settings:Default",
            "Default Settings Services",
            "Default server/tenant/role settings provider + layered effective-settings resolver")
    {
        Configuration(builder =>
        {

            builder.Services.Configure<List<ServerSettingConfiguration>>(builder.Configuration.GetSection("Settings:ServerSetting"));
            builder.Services.Configure<List<TenantSettingConfiguration>>(builder.Configuration.GetSection("Settings:TenantSetting"));
            builder.Services.Configure<List<RoleSettingConfiguration>>(builder.Configuration.GetSection("Settings:RoleSetting"));
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {

            RegisterSettingProvider<ServerSettingConfiguration, ServerSettingConfigurationCommand>(builder.Services);
            RegisterSettingProvider<TenantSettingConfiguration, TenantSettingConfigurationCommand>(builder.Services);
            RegisterSettingProvider<RoleSettingConfiguration, RoleSettingConfigurationCommand>(builder.Services);
            builder.Services.TryAddSingleton<SettingsConfigurationProvider>();

            builder.Services.TryAddSingleton<IEffectiveSettingsProvider, DefaultEffectiveSettingsProvider>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    private static void RegisterSettingProvider<TConfig, TCommand>(IServiceCollection services)
        where TConfig : class, IGenericConfiguration
        where TCommand : ConfigurationCommandBase<TConfig>
    {
        services.TryAddSingleton<ImplementationConfigurationProviderBase<TConfig, TCommand>>(sp =>
            new ImplementationConfigurationProviderBase<TConfig, TCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                "ConfigurationDb",
                "settings"));
        services.TryAddSingleton<IServiceConfigurationProvider<TConfig>>(sp =>
            sp.GetRequiredService<ImplementationConfigurationProviderBase<TConfig, TCommand>>());
    }
}
