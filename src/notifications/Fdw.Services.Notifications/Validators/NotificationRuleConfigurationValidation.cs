using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Notifications.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Notifications.Validators;

/// <summary>States that <see cref="NotificationRuleImplementationConfiguration"/> is checked by <see cref="NotificationRuleConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "NotificationRuleImplementationConfiguration")]
public sealed class NotificationRuleConfigurationValidation : ValidatedConfiguration<NotificationRuleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationRuleConfigurationValidation"/> class.</summary>
    public NotificationRuleConfigurationValidation()
        : base("NotificationRuleImplementationConfiguration", new NotificationRuleConfigurationValidator())
    {
    }
}
