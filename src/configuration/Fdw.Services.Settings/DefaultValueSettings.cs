using System;
using System.Collections.Generic;

namespace Fdw.Services.Settings;

/// <summary>
/// The settings that hold the defaults of values a caller may leave out (FDW-3580).
/// </summary>
/// <remarks>
/// Mike, 2026-11-04: "I don't care what you set the default to as long as it is in configuration and not hardcoded". Each is read through
/// <see cref="InstanceDefault"/> for the caller's tenant, so the scope chain applies; one that is missing is a failure naming it, never a
/// number in code. The values are rows in the baseline runtime seed.
/// </remarks>
public static partial class DefaultValueSettings
{
    /// <summary>The setting that holds whether a connection that states none is health checked.</summary>
    public const string DefaultConnectionHealthCheckEnabled = "DefaultConnectionHealthCheckEnabled";

    /// <summary>The setting that holds whether a connection that states none is health checked when the host starts.</summary>
    public const string DefaultConnectionHealthCheckOnStartup = "DefaultConnectionHealthCheckOnStartup";

    /// <summary>The setting that holds whether a schedule created without saying runs.</summary>
    public const string DefaultScheduleIsEnabled = "DefaultScheduleIsEnabled";

    /// <summary>The setting that holds how many times a schedule that states none retries a failed run.</summary>
    public const string DefaultScheduleMaxRetries = "DefaultScheduleMaxRetries";

    /// <summary>The setting that holds the seconds a schedule that states none waits before a retry.</summary>
    public const string DefaultScheduleRetryDelaySeconds = "DefaultScheduleRetryDelaySeconds";

    /// <summary>The setting that holds the instance name of a Quartz scheduler that states none.</summary>
    public const string DefaultQuartzInstanceName = "DefaultQuartzInstanceName";

    /// <summary>The setting that holds the threads of a Quartz scheduler that states none.</summary>
    public const string DefaultQuartzThreadCount = "DefaultQuartzThreadCount";

    /// <summary>The setting that holds whether a notification rule created without saying is enabled.</summary>
    public const string DefaultNotificationRuleIsEnabled = "DefaultNotificationRuleIsEnabled";

    /// <summary>The setting that holds the severity of a notification rule that states none.</summary>
    public const string DefaultNotificationRuleSeverity = "DefaultNotificationRuleSeverity";

    /// <summary>The setting that holds how the conditions of a notification rule that states none combine.</summary>
    public const string DefaultNotificationRuleConditionOperator = "DefaultNotificationRuleConditionOperator";

    /// <summary>The setting that holds whether a notification list created without saying is enabled.</summary>
    public const string DefaultNotificationListIsEnabled = "DefaultNotificationListIsEnabled";

    /// <summary>The setting that holds whether a notification list member added without saying is enabled.</summary>
    public const string DefaultNotificationListMemberIsEnabled = "DefaultNotificationListMemberIsEnabled";

    /// <summary>The setting that holds whether a notification recipient added without saying is enabled.</summary>
    public const string DefaultNotificationRecipientIsEnabled = "DefaultNotificationRecipientIsEnabled";

    /// <summary>The setting that holds whether a notification preference saved without saying is enabled.</summary>
    public const string DefaultNotificationPreferenceIsEnabled = "DefaultNotificationPreferenceIsEnabled";

    /// <summary>The setting that holds whether a notification channel created without saying is enabled.</summary>
    public const string DefaultNotificationChannelIsEnabled = "DefaultNotificationChannelIsEnabled";

    /// <summary>The setting that holds the seconds a webhook notification that states none waits for an answer.</summary>
    public const string DefaultWebhookTimeoutSeconds = "DefaultWebhookTimeoutSeconds";

    /// <summary>The setting that holds how many times a webhook notification that states none retries.</summary>
    public const string DefaultWebhookRetryCount = "DefaultWebhookRetryCount";

    /// <summary>The setting that holds the HTTP method of a webhook notification that states none.</summary>
    public const string DefaultWebhookMethod = "DefaultWebhookMethod";

    /// <summary>The setting that holds the content type of a webhook notification that states none.</summary>
    public const string DefaultWebhookContentType = "DefaultWebhookContentType";

    /// <summary>The setting that holds the seconds a Teams notification that states none waits for an answer.</summary>
    public const string DefaultTeamsTimeoutSeconds = "DefaultTeamsTimeoutSeconds";

    /// <summary>The setting that holds whether a Teams notification that states none sends adaptive cards.</summary>
    public const string DefaultTeamsUseAdaptiveCards = "DefaultTeamsUseAdaptiveCards";

    /// <summary>The setting that holds the SMTP port of an email notification that states none.</summary>
    public const string DefaultEmailSmtpPort = "DefaultEmailSmtpPort";

    /// <summary>The setting that holds whether an email notification that states none uses SSL.</summary>
    public const string DefaultEmailUseSsl = "DefaultEmailUseSsl";

    /// <summary>The setting that holds the log level of a console notification that states none.</summary>
    public const string DefaultConsoleLogLevel = "DefaultConsoleLogLevel";

    /// <summary>The setting that holds the message severity of a system notification channel that states none.</summary>
    public const string DefaultSystemNotificationSeverity = "DefaultSystemNotificationSeverity";

    /// <summary>The setting that holds the message type of a system notification channel that states none.</summary>
    public const string DefaultSystemNotificationMessageType = "DefaultSystemNotificationMessageType";

    /// <summary>The setting that holds the severity of a message one person sends another.</summary>
    public const string DefaultPersonMessageSeverity = "DefaultPersonMessageSeverity";

    private static readonly Dictionary<string, SettingDefinition> ByName =
        new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [DefaultConnectionHealthCheckEnabled] = new("Boolean", "Whether a connection that states none is health checked."),
            [DefaultConnectionHealthCheckOnStartup] = new("Boolean", "Whether a connection that states none is health checked when the host starts."),
            [DefaultScheduleIsEnabled] = new("Boolean", "Whether a schedule created without saying runs."),
            [DefaultScheduleMaxRetries] = new("Int32", "How many times a schedule that states none retries a failed run."),
            [DefaultScheduleRetryDelaySeconds] = new("Int32", "The seconds a schedule that states none waits before a retry."),
            [DefaultQuartzInstanceName] = new("String", "The instance name of a Quartz scheduler that states none."),
            [DefaultQuartzThreadCount] = new("Int32", "The threads of a Quartz scheduler that states none."),
            [DefaultNotificationRuleIsEnabled] = new("Boolean", "Whether a notification rule created without saying is enabled."),
            [DefaultNotificationRuleSeverity] = new("String", "The severity of a notification rule that states none."),
            [DefaultNotificationRuleConditionOperator] = new("String", "How the conditions of a notification rule that states none combine."),
            [DefaultNotificationListIsEnabled] = new("Boolean", "Whether a notification list created without saying is enabled."),
            [DefaultNotificationListMemberIsEnabled] = new("Boolean", "Whether a notification list member added without saying is enabled."),
            [DefaultNotificationRecipientIsEnabled] = new("Boolean", "Whether a notification recipient added without saying is enabled."),
            [DefaultNotificationPreferenceIsEnabled] = new("Boolean", "Whether a notification preference saved without saying is enabled."),
            [DefaultNotificationChannelIsEnabled] = new("Boolean", "Whether a notification channel created without saying is enabled."),
            [DefaultWebhookTimeoutSeconds] = new("Int32", "The seconds a webhook notification that states none waits for an answer."),
            [DefaultWebhookRetryCount] = new("Int32", "How many times a webhook notification that states none retries."),
            [DefaultWebhookMethod] = new("String", "The HTTP method of a webhook notification that states none."),
            [DefaultWebhookContentType] = new("String", "The content type of a webhook notification that states none."),
            [DefaultTeamsTimeoutSeconds] = new("Int32", "The seconds a Teams notification that states none waits for an answer."),
            [DefaultTeamsUseAdaptiveCards] = new("Boolean", "Whether a Teams notification that states none sends adaptive cards."),
            [DefaultEmailSmtpPort] = new("Int32", "The SMTP port of an email notification that states none."),
            [DefaultEmailUseSsl] = new("Boolean", "Whether an email notification that states none uses SSL."),
            [DefaultConsoleLogLevel] = new("String", "The log level of a console notification that states none."),
            [DefaultSystemNotificationSeverity] = new("String", "The message severity of a system notification channel that states none."),
            [DefaultSystemNotificationMessageType] = new("String", "The message type of a system notification channel that states none."),
            [DefaultPersonMessageSeverity] = new("String", "The severity of a message one person sends another."),
        };

    /// <summary>Attempts to resolve the definition of a default-value setting by name.</summary>
    /// <param name="name">The setting's name.</param>
    /// <param name="definition">The definition, when the name is one.</param>
    /// <returns><see langword="true"/> when the name is a default-value setting.</returns>
    /// <remarks>
    /// FDW-3823 splits the defaults by share, one partial file each (DefaultValueSettings.Data, .Pipelines, .Tenancy, .Web and
    /// .Themes), so the sessions that move them never edit the same lines.
    /// </remarks>
    /// <remarks>
    /// A type's own setting, <c>{setting}.{typeName}</c> (<see cref="InstanceDefault.ForType"/>), has its server-wide setting's definition
    /// (FDW-3823: a server default, overridden per connection or service type).
    /// </remarks>
    internal static bool TryGet(string name, out SettingDefinition definition)
        => TryGetDeclared(name, out definition) ||
           (name.IndexOf('.', StringComparison.Ordinal) is var dot and > 0 && TryGetDeclared(name.Substring(0, dot), out definition));

    private static bool TryGetDeclared(string name, out SettingDefinition definition)
        => ByName.TryGetValue(name, out definition) ||
           DataDefaults.TryGetValue(name, out definition) ||
           PipelinesDefaults.TryGetValue(name, out definition) ||
           TenancyDefaults.TryGetValue(name, out definition) ||
           WebDefaults.TryGetValue(name, out definition) ||
           ThemesDefaults.TryGetValue(name, out definition);
}
