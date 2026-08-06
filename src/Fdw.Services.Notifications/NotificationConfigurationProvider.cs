using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Commands;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Notifications;

/// <summary>
/// Configuration provider for notifications. Thin wrapper over
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>. Also registers the
/// NotificationRule sub-provider (separate config category, same domain).
/// </summary>
public class NotificationConfigurationProvider : DefaultConfigurationProvider<NotificationConfiguration, NotificationConfigurationCommand>
{
    /// <summary>
    /// Registers the NotificationConfigurationProvider and child rule provider with DI, targeting
    /// this domain's own default location. To override the header provider's location, call
    /// <c>SetConfiguration</c> on the resolved singleton. Pure Phase-1b registration — no
    /// IConfiguration; IOptions binding is a Phase-1a concern that lives in each consuming
    /// <c>[ServiceTypeOption].Configure</c>, not here.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        // Why: per-user notification toggles are plain data in ConfigurationDb (notify schema),
        // read/written via the standard DataGateway — this replaces the no-op echo endpoints.
        services.TryAddScoped<IUserNotificationPreferenceService, SqlUserNotificationPreferenceService>();

        services.TryAddSingleton<NotificationConfigurationProvider>(sp =>
            new NotificationConfigurationProvider(
                sp.GetService<ILogger<NotificationConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        services.TryAddSingleton<DefaultConfigurationProvider<NotificationConfiguration, NotificationConfigurationCommand>>(
            sp => sp.GetRequiredService<NotificationConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<NotificationConfiguration>>(
            sp => sp.GetRequiredService<NotificationConfigurationProvider>());

        // Why literal "ConfigurationDb"/"notify": this child rule provider is a plain
        // DefaultConfigurationProvider<,> instance (not a domain-specific subclass), so there is no
        // per-domain constructor default to fall back on — this is the domain's own default location.
        services.TryAddSingleton<IServiceConfigurationProvider<NotificationRuleConfiguration>>(sp =>
            new DefaultConfigurationProvider<NotificationRuleConfiguration, NotificationRuleConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<NotificationRuleConfiguration, NotificationRuleConfigurationCommand>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                "ConfigurationDb", "notify",
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
    }

    /// <summary>Initializes a new instance of the <see cref="NotificationConfigurationProvider"/> class.</summary>
    public NotificationConfigurationProvider(
        ILogger<NotificationConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "notify",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<NotificationConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
