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
    typeof(NotificationTypeBase<IGenericNotification, INotificationFactory<IGenericNotification, NotificationConfiguration>, NotificationConfiguration>),
    typeof(INotificationType),
    typeof(NotificationTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(IGenericNotification),
    ConfigurationInterface = typeof(NotificationConfiguration),
    ConfigurationType = typeof(NotificationConfiguration),
    ProviderType = typeof(DefaultServiceProvider<IGenericNotification, NotificationConfiguration, INotificationFactory<IGenericNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>),
    ProviderInterface = typeof(IFdwServiceProvider<IGenericNotification, NotificationConfiguration>),
    ServiceCategory = "Notification")]
public partial class NotificationTypes
    : ServiceTypeCollectionBase<
        NotificationTypeBase<IGenericNotification, INotificationFactory<IGenericNotification, NotificationConfiguration>, NotificationConfiguration>,
        INotificationType>
{
    // Configure(), Register() and Initialize() are source-generated

    /// <summary>
    /// Sets this collection's Register body: the option sweep, then this domain's provider.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names it,
    /// so the body that registers it is written here beside the declaration. Setting it as the phase's
    /// body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the
    /// sweep and this registration together, which is the correct semantic for a host taking over phase 2.
    /// </remarks>
    static NotificationTypes()
    {
        var sweepOptions = RegisterFunc;
        Registration((builder, loggerFactory) =>
        {
            sweepOptions(builder, loggerFactory);
            builder.Services.AddScoped<IFdwServiceProvider<IGenericNotification, NotificationConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<IGenericNotification, NotificationConfiguration, INotificationFactory<IGenericNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<IGenericNotification, NotificationConfiguration, INotificationFactory<IGenericNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<IGenericNotification, NotificationConfiguration, INotificationFactory<IGenericNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>>.Instance);
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger("NotificationTypes");
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<NotificationConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.RegisterParentProvider(cfgProvider);
                        if (!parentResult.IsSuccess && stLogger != null)
                            ServiceTypeLog.FactoryRegistrationFailed(stLogger, "NotificationTypes", parentResult.CurrentMessage ?? "NotificationTypes");
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    if (stLogger != null) ServiceTypeLog.FactoryRegistrationException(stLogger, ex, "NotificationTypes");
                    throw;
                }
                return provider;
            });
            return builder;
        });
    }
}
