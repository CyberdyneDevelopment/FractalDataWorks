using System;
using System.Collections.Generic;

namespace Fdw.Services.Settings;

/// <summary>
/// Well-known setting name constants used throughout the system, plus the
/// authoritative registry of server settings the platform recognizes.
/// These names correspond to <c>ServerSetting.SettingName</c> values in the database.
/// </summary>
public static class SettingDefinitions
{
    /// <summary>
    /// Maximum number of items returned by paginated list endpoints.
    /// DataType: Int32. Default: 100. Min: 1. Max: 10000.
    /// </summary>
    public const string MaxPaginationSize = "MaxPaginationSize";

    /// <summary>
    /// Maximum number of concurrent queries allowed per tenant.
    /// DataType: Int32. Default: 10. Min: 1. Max: 100.
    /// </summary>
    public const string MaxConcurrentQueries = "MaxConcurrentQueries";

    /// <summary>
    /// Maximum number of rows returned by data preview endpoints.
    /// DataType: Int32. Default: 1000. Min: 1. Max: 100000.
    /// </summary>
    public const string MaxPreviewRows = "MaxPreviewRows";

    /// <summary>
    /// Default timeout in milliseconds for data operations.
    /// DataType: Int32. Default: 30000. Min: 1000. Max: 600000.
    /// </summary>
    public const string DefaultTimeoutMs = "DefaultTimeoutMs";

    /// <summary>
    /// Whether data lineage tracking is enabled.
    /// DataType: Boolean. Default: true.
    /// </summary>
    public const string EnableLineageTracking = "EnableLineageTracking";

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// DataType: Int32. Default: 3. Min: 0. Max: 10.
    /// </summary>
    public const string MaxRetryAttempts = "MaxRetryAttempts";

    /// <summary>Human-readable system / instance name shown in the management UI.</summary>
    public const string SystemName = "SystemName";

    /// <summary>Default display timezone for the management UI.</summary>
    public const string Timezone = "Timezone";

    /// <summary>Default date-format pattern for the management UI.</summary>
    public const string DateFormat = "DateFormat";

    /// <summary>Idle session timeout, in minutes.</summary>
    public const string SessionTimeoutMinutes = "SessionTimeoutMinutes";

    /// <summary>Whether two-factor authentication is required for all users.</summary>
    public const string Enable2FA = "Enable2FA";

    /// <summary>Whether pipeline-failure notifications are enabled.</summary>
    public const string NotifyPipelineFailures = "NotifyPipelineFailures";

    /// <summary>Whether schedule-trigger notifications are enabled.</summary>
    public const string NotifyScheduleTriggers = "NotifyScheduleTriggers";

    /// <summary>Whether connection-issue notifications are enabled.</summary>
    public const string NotifyConnectionIssues = "NotifyConnectionIssues";

    /// <summary>Whether system-update notifications are enabled.</summary>
    public const string NotifySystemUpdates = "NotifySystemUpdates";

    private static readonly Dictionary<string, SettingDefinition> KnownSettings =
        new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [MaxPaginationSize] = new("Int32", "Maximum number of items returned by paginated list endpoints."),
            [MaxConcurrentQueries] = new("Int32", "Maximum number of concurrent queries allowed per tenant."),
            [MaxPreviewRows] = new("Int32", "Maximum number of rows returned by data preview endpoints."),
            [DefaultTimeoutMs] = new("Int32", "Default timeout in milliseconds for data operations."),
            [EnableLineageTracking] = new("Boolean", "Whether data lineage tracking is enabled."),
            [MaxRetryAttempts] = new("Int32", "Maximum number of retry attempts for transient failures."),
            [SystemName] = new("String", "Human-readable system / instance name shown in the management UI."),
            [Timezone] = new("String", "Default display timezone for the management UI."),
            [DateFormat] = new("String", "Default date-format pattern for the management UI."),
            [SessionTimeoutMinutes] = new("Int32", "Idle session timeout, in minutes."),
            [Enable2FA] = new("Boolean", "Whether two-factor authentication is required for all users."),
            [NotifyPipelineFailures] = new("Boolean", "Whether pipeline-failure notifications are enabled."),
            [NotifyScheduleTriggers] = new("Boolean", "Whether schedule-trigger notifications are enabled."),
            [NotifyConnectionIssues] = new("Boolean", "Whether connection-issue notifications are enabled."),
            [NotifySystemUpdates] = new("Boolean", "Whether system-update notifications are enabled."),
        };

    /// <summary>
    /// Attempts to resolve a well-known server setting definition by name.
    /// Returns <c>false</c> for names that are not part of the platform registry.
    /// </summary>
    public static bool TryGet(string name, out SettingDefinition definition)
        => KnownSettings.TryGetValue(name, out definition!);
}
