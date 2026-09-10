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
/// The source generator populates this with discovered [Implementation] types.
/// </summary>
/// <remarks>
/// PlatformServices runs Register(services), which invokes each option's Register phase.
/// Use <c>NotificationServiceTypes.ByName("Email")</c> to look up specific types.
/// </remarks>
[ServiceTypeCollection(
    typeof(NotificationTypeBase<IPlatformNotification, INotificationFactory<IPlatformNotification, INotificationImplementationConfiguration>, INotificationImplementationConfiguration>),
    typeof(INotificationType),
    typeof(NotificationServiceTypes),
    ServiceInterface = typeof(IPlatformNotification),
    ConfigurationInterface = typeof(INotificationConfiguration),
    ProviderType = typeof(NotificationServiceProvider),
    ProviderInterface = typeof(INotificationServiceProvider),
    ServiceCategory = "Notification")]
public partial class NotificationServiceTypes
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
    static NotificationServiceTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(INotificationServiceProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<NotificationServiceTypes>() ?? NullLogger<NotificationServiceTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;
            // Notification configuration, registered once for the domain here rather
            // than by every caller that needs it.

            builder.Services.TryAddScoped<IUserNotificationPreferenceService, SqlUserNotificationPreferenceService>();

            builder.Services.TryAddSingleton<NotificationConfigurationProvider>();
            builder.Services.TryAddSingleton<INotificationConfigurationProvider>(
                sp => sp.GetRequiredService<NotificationConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<INotificationImplementationConfiguration>>(
                sp => sp.GetRequiredService<NotificationConfigurationProvider>());

            builder.Services.TryAddSingleton<NotificationRuleImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<INotificationRuleImplementationConfigurationProvider>(sp => sp.GetRequiredService<NotificationRuleImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<NotificationRuleConfigurationProvider>(sp =>
            {
                var domain = new NotificationRuleConfigurationProvider(
                    sp.GetRequiredService<ILogger<NotificationRuleConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("NotificationRule", sp.GetRequiredService<INotificationRuleImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<INotificationRuleConfigurationProvider>(sp => sp.GetRequiredService<NotificationRuleConfigurationProvider>());


            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(NotificationServiceTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(NotificationServiceTypes), providerService);

            builder.Services.AddScoped<INotificationServiceProvider>(sp =>
            {
                var provider = new NotificationServiceProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<NotificationServiceProvider>()
                    ?? NullLogger<NotificationServiceProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<NotificationServiceTypes>()
                    ?? NullLogger<NotificationServiceTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(NotificationServiceTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<INotificationConfigurationProvider>() is { } cfgProvider)
                    {
                        var domainResult = provider.Register(cfgProvider);
                        if (domainResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(NotificationServiceTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(NotificationServiceTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                    }
                    else
                    {
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(NotificationServiceTypes),
                            provider.GetType().Name,
                            typeof(IImplementationConfigurationProvider<INotificationImplementationConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(NotificationServiceTypes));
                    throw;
                }
                return provider;
            });

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(NotificationServiceTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(NotificationServiceTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
