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

        // Why a local: this closed generic is the DI key a consumer injects, and it is reported at
        // three points below — the deferred declaration, the milestone, and the zero-option warning.
        // Written out three times it is three chances for them to disagree.
        var providerService = typeof(IFdwServiceProvider<IGenericNotification, NotificationConfiguration>).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<NotificationTypes>() ?? NullLogger<NotificationTypes>.Instance;

            // Why the result is read: this replacement calls the func it captured, and discarding
            // what that returned meant an option that failed to register was followed by this body
            // registering the provider anyway and reporting success.
            var registered = sweepOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionSweepCompleted(log, nameof(NotificationTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(NotificationTypes), providerService);

            builder.Services.AddScoped<IFdwServiceProvider<IGenericNotification, NotificationConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<IGenericNotification, NotificationConfiguration, INotificationFactory<IGenericNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<IGenericNotification, NotificationConfiguration, INotificationFactory<IGenericNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<IGenericNotification, NotificationConfiguration, INotificationFactory<IGenericNotification, NotificationConfiguration>, IServiceConfigurationProvider<NotificationConfiguration>>>.Instance);

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
