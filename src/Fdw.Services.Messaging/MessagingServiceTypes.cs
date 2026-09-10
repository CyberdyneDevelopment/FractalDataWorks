using Fdw.Configuration;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging;

/// <summary>
/// ServiceTypeCollection for messaging service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(MessagingServiceTypeBase),
    typeof(IMessagingServiceType),
    typeof(MessagingServiceTypes),
    ServiceCategory = "Messaging")]
public partial class MessagingServiceTypes : ServiceTypeCollectionBase<
    MessagingServiceTypeBase,
    IMessagingServiceType>
{
    /// <summary>
    /// The connection the domain's <c>msg.Messaging</c> rows are read from and written to.
    /// </summary>
    /// <remarks>
    /// PlatformConfiguration, because that is where every domain header lives — the row naming a
    /// messaging service and its store/path lives beside every other configured domain, even though
    /// the messages themselves live in whichever store that row names.
    /// </remarks>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    /// <summary>
    /// Sets this collection's Register body: the option collect, then this domain's configuration provider.
    /// </summary>
    static MessagingServiceTypes()
    {
        var collectOptions = RegisterFunc;

        Registration((builder, loggerFactory) =>
        {
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            builder.Services.TryAddSingleton<MessagingImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IMessagingImplementationConfigurationProvider>(
                sp => sp.GetRequiredService<MessagingImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<MessagingConfigurationProvider>(sp =>
            {
                var domain = new MessagingConfigurationProvider(
                    sp.GetRequiredService<ILogger<MessagingConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("Messaging", sp.GetRequiredService<IMessagingImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IMessagingConfigurationProvider>(
                sp => sp.GetRequiredService<MessagingConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<IMessagingImplementationConfiguration>>(
                sp => sp.GetRequiredService<MessagingConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
