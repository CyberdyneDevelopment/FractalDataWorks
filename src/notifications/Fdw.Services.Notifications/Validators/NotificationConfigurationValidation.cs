using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Notifications.Validators;

/// <summary>States that <see cref="INotificationImplementationConfiguration"/> (and what derives from or implements it) is checked by <see cref="NotificationConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "INotificationImplementationConfiguration")]
public sealed class NotificationConfigurationValidation : ValidatedConfiguration<INotificationImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationConfigurationValidation"/> class.</summary>
    public NotificationConfigurationValidation()
        : base("INotificationImplementationConfiguration", new NotificationConfigurationValidator())
    {
    }
}
