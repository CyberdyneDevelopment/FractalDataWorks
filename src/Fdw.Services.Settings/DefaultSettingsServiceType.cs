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
/// Default settings service type. Registers the gateway-backed ServerSetting, TenantSetting and RoleSetting providers
/// (server/tenant/role settings) and the layered IEffectiveSettingsProvider.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SettingsServiceTypes), "Default")]
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

            builder.Services.Configure<List<ServerSettingImplementationConfiguration>>(builder.Configuration.GetSection("Settings:ServerSetting"));
            builder.Services.Configure<List<TenantSettingImplementationConfiguration>>(builder.Configuration.GetSection("Settings:TenantSetting"));
            builder.Services.Configure<List<RoleSettingImplementationConfiguration>>(builder.Configuration.GetSection("Settings:RoleSetting"));
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<RoleSettingImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IRoleSettingImplementationConfigurationProvider>(sp => sp.GetRequiredService<RoleSettingImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<RoleSettingConfigurationProvider>(sp =>
            {
                var domain = new RoleSettingConfigurationProvider(
                    sp.GetRequiredService<ILogger<RoleSettingConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("RoleSetting", sp.GetRequiredService<IRoleSettingImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IRoleSettingConfigurationProvider>(sp => sp.GetRequiredService<RoleSettingConfigurationProvider>());

            builder.Services.TryAddSingleton<TenantSettingImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<ITenantSettingImplementationConfigurationProvider>(sp => sp.GetRequiredService<TenantSettingImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<TenantSettingConfigurationProvider>(sp =>
            {
                var domain = new TenantSettingConfigurationProvider(
                    sp.GetRequiredService<ILogger<TenantSettingConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("TenantSetting", sp.GetRequiredService<ITenantSettingImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ITenantSettingConfigurationProvider>(sp => sp.GetRequiredService<TenantSettingConfigurationProvider>());


            builder.Services.TryAddSingleton<ServerSettingImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IServerSettingImplementationConfigurationProvider>(
                sp => sp.GetRequiredService<ServerSettingImplementationConfigurationProvider>());
            // The domain is built with its one implementation registered into it, as RoleMapping is:
            // a domain provider with nothing registered answers every read with NoImplementationProvider.
            builder.Services.TryAddSingleton<ServerSettingConfigurationProvider>(sp =>
            {
                var domain = new ServerSettingConfigurationProvider(
                    sp.GetRequiredService<ILogger<ServerSettingConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("ServerSetting", sp.GetRequiredService<IServerSettingImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IServerSettingConfigurationProvider>(
                sp => sp.GetRequiredService<ServerSettingConfigurationProvider>());

            builder.Services.TryAddSingleton<IEffectiveSettingsProvider, DefaultEffectiveSettingsProvider>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
