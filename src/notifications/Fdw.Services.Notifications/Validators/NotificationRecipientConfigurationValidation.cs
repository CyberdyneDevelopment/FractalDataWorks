using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Notifications.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Notifications.Validators;

/// <summary>States that <see cref="NotificationRecipientConfiguration"/> (and what derives from or implements it) is checked by <see cref="NotificationRecipientConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "NotificationRecipientConfiguration")]
public sealed class NotificationRecipientConfigurationValidation : ValidatedConfiguration<NotificationRecipientConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationRecipientConfigurationValidation"/> class.</summary>
    public NotificationRecipientConfigurationValidation()
        : base("NotificationRecipientConfiguration", new NotificationRecipientConfigurationValidator())
    {
    }
}
