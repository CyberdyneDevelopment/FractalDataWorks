using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Notifications.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Fdw.Data.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Commands;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace Fdw.Services.Notifications;

/// <summary>
/// ServiceTypeCollection for all notification service implementations.
/// The source generator populates this with discovered [ServiceTypeOption] types.
/// </summary>
/// <remarks>
/// PlatformServices runs Register(services), which invokes each option's Register phase.
/// Use <c>NotificationTypes.ByName("Email")</c> to look up specific types.
/// </remarks>
[ServiceTypeCollection(
    typeof(NotificationTypeBase<IPlatformNotification, INotificationFactory<IPlatformNotification, INotificationImplementationConfiguration>, INotificationImplementationConfiguration>),
    typeof(INotificationType),
    typeof(NotificationTypes),
    ServiceInterface = typeof(IPlatformNotification),
    ConfigurationInterface = typeof(NotificationConfiguration),
    ProviderType = typeof(NotificationServiceProvider),
    ProviderInterface = typeof(INotificationServiceProvider),
    ServiceCategory = "Notification")]
public partial class NotificationTypes
    : ServiceTypeCollectionBase<
        NotificationTypeBase<IPlatformNotification, INotificationFactory<IPlatformNotification, INotificationImplementationConfiguration>, INotificationImplementationConfiguration>,
        INotificationType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    // Configure(), Register() and Initialize() are source-generated

    /// <summary>
    /// Sets this collection's Register body: the option collect, then this domain's provider.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names it,
    /// so the body that registers it is written here beside the declaration. Setting it as the phase's
    /// body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the
    /// collect and this registration together, which is the correct semantic for a host taking over phase 2.
    /// </remarks>
    static NotificationTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(INotificationServiceProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<NotificationTypes>() ?? NullLogger<NotificationTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;
            // Notification configuration, registered once for the domain here rather
            // than by every caller that needs it.

            builder.Services.TryAddScoped<IUserNotificationPreferenceService, SqlUserNotificationPreferenceService>();

            builder.Services.TryAddSingleton<INotificationConfigurationProvider>(sp =>
                new NotificationConfigurationProvider(
                    sp.GetService<ILogger<NotificationConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));
            builder.Services.TryAddSingleton<NotificationConfigurationProvider>(
                sp => (NotificationConfigurationProvider)sp.GetRequiredService<INotificationConfigurationProvider>());
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<NotificationConfiguration, NotificationConfigurationCommand>>(
                sp => sp.GetRequiredService<NotificationConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<NotificationConfiguration>>(
                sp => sp.GetRequiredService<NotificationConfigurationProvider>());

            builder.Services.TryAddSingleton<IServiceConfigurationProvider<NotificationRuleConfiguration>>(sp =>
                new ImplementationConfigurationProviderBase<NotificationRuleConfiguration, NotificationRuleConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<NotificationRuleConfiguration, NotificationRuleConfigurationCommand>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    "ConfigurationDb", "notify"));

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(NotificationTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(NotificationTypes), providerService);

            builder.Services.AddScoped<INotificationServiceProvider>(sp =>
            {
                var provider = new NotificationServiceProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<NotificationServiceProvider>()
                    ?? NullLogger<NotificationServiceProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<NotificationTypes>()
                    ?? NullLogger<NotificationTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(NotificationTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<INotificationConfigurationProvider>() is { } cfgProvider)
                    {
                        var domainResult = provider.Register(cfgProvider);
                        if (domainResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(NotificationTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(NotificationTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                    }
                    else
                    {
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(NotificationTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<NotificationConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(NotificationTypes));
                    throw;
                }
                return provider;
            });

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(NotificationTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(NotificationTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
