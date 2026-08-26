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
    typeof(NotificationTypeBase<IPlatformNotification, INotificationFactory<IPlatformNotification, NotificationConfiguration>, NotificationConfiguration>),
    typeof(INotificationType),
    typeof(NotificationTypes),
    ServiceInterface = typeof(IPlatformNotification),
    ConfigurationInterface = typeof(NotificationConfiguration),
    ConfigurationType = typeof(NotificationConfiguration),
    ProviderType = typeof(NotificationServiceProvider),
    ProviderInterface = typeof(INotificationServiceProvider),
    ServiceCategory = "Notification")]
public partial class NotificationTypes
    : ServiceTypeCollectionBase<
        NotificationTypeBase<IPlatformNotification, INotificationFactory<IPlatformNotification, NotificationConfiguration>, NotificationConfiguration>,
        INotificationType>
{
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

        // Why a local: this closed generic is the DI key a consumer injects, and it is reported at
        // three points below — the deferred declaration, the milestone, and the zero-option warning.
        // Written out three times it is three chances for them to disagree.
        var providerService = typeof(IPlatformServiceProvider<IPlatformNotification, NotificationConfiguration>).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<NotificationTypes>() ?? NullLogger<NotificationTypes>.Instance;

            // Why the result is read: this replacement calls the func it captured, and discarding
            // what that returned meant an option that failed to register was followed by this body
            // registering the provider anyway and reporting success.
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;
            // Notification configuration, registered once for the domain here rather
            // than by every caller that needs it.

            // Why: per-user notification toggles are plain data in ConfigurationDb (notify schema),
            // read/written via the standard DataGateway — this replaces the no-op echo endpoints.
            builder.Services.TryAddScoped<IUserNotificationPreferenceService, SqlUserNotificationPreferenceService>();

            builder.Services.TryAddSingleton<NotificationConfigurationProvider>(sp =>
                new NotificationConfigurationProvider(
                    sp.GetService<ILogger<NotificationConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>()));
            builder.Services.TryAddSingleton<DefaultConfigurationProvider<NotificationConfiguration, NotificationConfigurationCommand>>(
                sp => sp.GetRequiredService<NotificationConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<NotificationConfiguration>>(
                sp => sp.GetRequiredService<NotificationConfigurationProvider>());

            // Why literal "ConfigurationDb"/"notify": this child rule provider is a plain
            // DefaultConfigurationProvider<,> instance (not a domain-specific subclass), so there is no
            // per-domain constructor default to fall back on — this is the domain's own default location.
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<NotificationRuleConfiguration>>(sp =>
                new DefaultConfigurationProvider<NotificationRuleConfiguration, NotificationRuleConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<NotificationRuleConfiguration, NotificationRuleConfigurationCommand>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    "ConfigurationDb", "notify"));

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(NotificationTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(NotificationTypes), providerService);

            builder.Services.AddScoped<IPlatformServiceProvider<IPlatformNotification, NotificationConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<IPlatformNotification, NotificationConfiguration, INotificationFactory<IPlatformNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<IPlatformNotification, NotificationConfiguration, INotificationFactory<IPlatformNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<IPlatformNotification, NotificationConfiguration, INotificationFactory<IPlatformNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>>.Instance);

                // Why ILogger<NotificationTypes> and not CreateLogger("NotificationTypes"): SourceContext then
                // carries the namespace-qualified collection, and the category cannot drift from the
                // type it claims to name. The provider logs its own lines under its own type, so the
                // two layers read base-then-derived rather than collapsing onto one category.
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<NotificationTypes>()
                    ?? NullLogger<NotificationTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(NotificationTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<NotificationConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.Register(cfgProvider);
                        if (parentResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(NotificationTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(NotificationTypes), provider.GetType().Name, cfgProvider.GetType().Name, parentResult.CurrentMessage);
                    }
                    else
                    {
                        // Why Critical, and why the collection says it rather than the provider: from
                        // inside the provider a null parent is indistinguishable from a domain that needs
                        // none. This is the one place that knows one was meant to arrive, and without it
                        // the domain fails every lookup by name for the life of the scope with nothing
                        // pointing back here.
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(NotificationTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<NotificationConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(NotificationTypes));
                    throw;
                }
                return provider;
            });

            // Why the milestone comes after the registration and not before: it states that the domain
            // finished phase 2, which is only true once the provider is actually in the container.
            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(NotificationTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(NotificationTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
