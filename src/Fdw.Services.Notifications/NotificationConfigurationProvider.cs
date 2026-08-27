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
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>. Also registers the
/// NotificationRule sub-provider (separate config category, same domain).
/// </summary>
public class NotificationConfigurationProvider
    : ServiceConfigurationProviderBase<
          NotificationConfiguration,
          INotificationImplementationConfiguration,
          NotificationConfigurationCommand>,
      INotificationConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="NotificationConfigurationProvider"/> class.</summary>
    public NotificationConfigurationProvider(
        ILogger<NotificationConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "notify")
        : base(logger ?? NullLogger<NotificationConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override NotificationConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
