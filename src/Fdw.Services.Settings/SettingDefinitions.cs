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

    /// <summary>The credential service the users domain resolves credentials through.</summary>
    public const string UsersCredentialServiceName = "UsersCredentialServiceName";

    /// <summary>The algorithm passwords are hashed with.</summary>
    public const string PasswordHashAlgorithm = "PasswordHashAlgorithm";

    /// <summary>How long a password stays valid, in days. Zero means it does not expire.</summary>
    public const string PasswordMaxAgeDays = "PasswordMaxAgeDays";

    /// <summary>Failed sign-ins before an account is locked out.</summary>
    public const string MaxFailedLoginAttempts = "MaxFailedLoginAttempts";

    /// <summary>How long an account stays locked out, in minutes.</summary>
    public const string LockoutDurationMinutes = "LockoutDurationMinutes";

    /// <summary>The role name that grants administrator authority.</summary>
    public const string AdminRoleName = "AdminRoleName";

    /// <summary>The role name that grants operator authority.</summary>
    public const string OperatorRoleName = "OperatorRoleName";

    /// <summary>The role name that grants read-only authority.</summary>
    public const string ViewerRoleName = "ViewerRoleName";

    /// <summary>The health monitor this host reports to.</summary>
    public const string HealthMonitorName = "HealthMonitorName";

    /// <summary>Whether calculation results are cached.</summary>
    public const string CalculationCacheEnabled = "CalculationCacheEnabled";

    /// <summary>How long a cached calculation result stays valid, in minutes.</summary>
    public const string CalculationCacheDefaultTtlMinutes = "CalculationCacheDefaultTtlMinutes";

    /// <summary>The longest TTL any calculation type may ask for, in minutes.</summary>
    public const string CalculationCacheMaxTtlMinutes = "CalculationCacheMaxTtlMinutes";

    /// <summary>Whether a data change evicts dependent calculation results.</summary>
    public const string CalculationCacheInvalidateOnDataChange = "CalculationCacheInvalidateOnDataChange";

    /// <summary>Whether the calculation cache is warmed at startup.</summary>
    public const string CalculationCacheWarmupOnStartup = "CalculationCacheWarmupOnStartup";

    /// <summary>The largest calculation result that may be cached, in bytes.</summary>
    public const string CalculationCacheMaxCachedResultSizeBytes = "CalculationCacheMaxCachedResultSizeBytes";

    /// <summary>Which cache implementation backs calculation results.</summary>
    public const string CalculationCacheProvider = "CalculationCacheProvider";

    /// <summary>The prefix every calculation cache key carries.</summary>
    public const string CalculationCacheKeyPrefix = "CalculationCacheKeyPrefix";

    /// <summary>The support address shown to a caller when a request fails.</summary>
    public const string SupportEmail = "SupportEmail";

    /// <summary>The support phone number shown to a caller when a request fails.</summary>
    public const string SupportPhone = "SupportPhone";

    /// <summary>The support portal shown to a caller when a request fails.</summary>
    public const string SupportPortalUrl = "SupportPortalUrl";

    /// <summary>The response time support commits to, in hours.</summary>
    public const string SupportExpectedResponseTimeHours = "SupportExpectedResponseTimeHours";

    /// <summary>What a caller is told to do when a request keeps failing.</summary>
    public const string SupportInstructions = "SupportInstructions";

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
            [UsersCredentialServiceName] = new("String", "The credential service the users domain resolves credentials through."),
            [PasswordHashAlgorithm] = new("String", "The algorithm passwords are hashed with."),
            [PasswordMaxAgeDays] = new("Int32", "How long a password stays valid, in days. Zero means it does not expire."),
            [MaxFailedLoginAttempts] = new("Int32", "Failed sign-ins before an account is locked out."),
            [LockoutDurationMinutes] = new("Int32", "How long an account stays locked out, in minutes."),
            [AdminRoleName] = new("String", "The role name that grants administrator authority."),
            [OperatorRoleName] = new("String", "The role name that grants operator authority."),
            [ViewerRoleName] = new("String", "The role name that grants read-only authority."),
            [HealthMonitorName] = new("String", "The health monitor this host reports to."),
            [CalculationCacheEnabled] = new("Boolean", "Whether calculation results are cached."),
            [CalculationCacheDefaultTtlMinutes] = new("Int32", "How long a cached calculation result stays valid, in minutes."),
            [CalculationCacheMaxTtlMinutes] = new("Int32", "The longest TTL any calculation type may ask for, in minutes."),
            [CalculationCacheInvalidateOnDataChange] = new("Boolean", "Whether a data change evicts dependent calculation results."),
            [CalculationCacheWarmupOnStartup] = new("Boolean", "Whether the calculation cache is warmed at startup."),
            [CalculationCacheMaxCachedResultSizeBytes] = new("Int32", "The largest calculation result that may be cached, in bytes."),
            [CalculationCacheProvider] = new("String", "Which cache implementation backs calculation results."),
            [CalculationCacheKeyPrefix] = new("String", "The prefix every calculation cache key carries."),
            [SupportEmail] = new("String", "The support address shown to a caller when a request fails."),
            [SupportPhone] = new("String", "The support phone number shown to a caller when a request fails."),
            [SupportPortalUrl] = new("String", "The support portal shown to a caller when a request fails."),
            [SupportExpectedResponseTimeHours] = new("Int32", "The response time support commits to, in hours."),
            [SupportInstructions] = new("String", "What a caller is told to do when a request keeps failing."),
        };

    /// <summary>
    /// Attempts to resolve a well-known server setting definition by name.
    /// Returns <c>false</c> for names that are not part of the platform registry.
    /// </summary>
    public static bool TryGet(string name, out SettingDefinition definition)
        => KnownSettings.TryGetValue(name, out definition!);
}
