using Fdw.Configuration;
using Fdw.Abstractions;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Marker interface for notification factories.
/// </summary>
public interface INotificationFactory
{
}

/// <summary>
/// Generic interface for notification factories with typed configuration.
/// </summary>
/// <typeparam name="TNotification">The type of notification service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface INotificationFactory<TNotification, TConfiguration> : INotificationFactory, IServiceFactory<TNotification, TConfiguration>
    where TNotification : IGenericNotification
    where TConfiguration : IGenericConfiguration
{
}
