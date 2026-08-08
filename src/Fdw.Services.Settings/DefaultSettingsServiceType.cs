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

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            RegisterSettingProvider<ServerSettingConfiguration, ServerSettingConfigurationCommand>(builder.Services);
            RegisterSettingProvider<TenantSettingConfiguration, TenantSettingConfigurationCommand>(builder.Services);
            RegisterSettingProvider<RoleSettingConfiguration, RoleSettingConfigurationCommand>(builder.Services);
            builder.Services.TryAddSingleton<SettingsConfigurationProvider>();

            builder.Services.TryAddSingleton<IEffectiveSettingsProvider, DefaultEffectiveSettingsProvider>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    // Why: the three setting tiers differ only by config/command type — one generic registrar keeps the
    // provider + its IServiceConfigurationProvider<T> forward in lockstep across all three. Mirrors
    // DefaultCalculationServiceType.RegisterTypedBodyProvider.
    private static void RegisterSettingProvider<TConfig, TCommand>(IServiceCollection services)
        where TConfig : class, IGenericConfiguration
        where TCommand : ConfigurationCommandBase<TConfig>
    {
        services.TryAddSingleton<DefaultConfigurationProvider<TConfig, TCommand>>(sp =>
            new DefaultConfigurationProvider<TConfig, TCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<TConfig, TCommand>>()
                    ?? NullLogger<DefaultConfigurationProvider<TConfig, TCommand>>.Instance,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                "ConfigurationDb",
                "settings",
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        services.TryAddSingleton<IServiceConfigurationProvider<TConfig>>(sp =>
            sp.GetRequiredService<DefaultConfigurationProvider<TConfig, TCommand>>());
    }
}
