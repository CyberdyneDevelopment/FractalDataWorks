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
    /// The largest page a paginated list endpoint returns, whatever <c>?take=</c> asks for.
    /// DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: a list request fails when it resolves to nothing positive. It resolves server, then
    /// tenant, then role, so a tenant or role can raise or lower it.
    /// </remarks>
    public const string MaxPaginationSize = "MaxPaginationSize";

    /// <summary>
    /// The page a paginated list endpoint returns when the request names no <c>?take=</c>.
    /// DataType: Int32. Must be positive; a value above <see cref="MaxPaginationSize"/> is capped to it.
    /// </summary>
    /// <remarks>
    /// Required, and resolved like <see cref="MaxPaginationSize"/>. It replaced a hard-coded 100 that
    /// nothing could override.
    /// </remarks>
    public const string DefaultPaginationSize = "DefaultPaginationSize";

    /// <summary>
    /// The largest page a caller authenticated with an API key may ask for (FDW-1209).
    /// DataType: Int32. Zero or more; 0 means no limit.
    /// </summary>
    /// <remarks>
    /// An API key is a service running unattended, and a page cut short is wrong data to it, so the limit that protects an
    /// interactive caller does not apply unless an operator sets one here. Required: a request from an API key fails when the
    /// setting is absent or negative, because an absent setting must never read as no limit. It resolves server, then tenant,
    /// then role, like <see cref="MaxPaginationSize"/>.
    /// </remarks>
    public const string ApiKeyMaxPaginationSize = "ApiKeyMaxPaginationSize";

    /// <summary>
    /// The page a caller authenticated with an API key gets when the request names no <c>take</c> (FDW-1209).
    /// DataType: Int32. Zero or more; 0 means no limit, so every row is returned.
    /// </summary>
    /// <remarks>
    /// Required, and resolved like <see cref="ApiKeyMaxPaginationSize"/>. A value above a non-zero
    /// <see cref="ApiKeyMaxPaginationSize"/> is capped to it.
    /// </remarks>
    public const string ApiKeyDefaultPaginationSize = "ApiKeyDefaultPaginationSize";

    /// <summary>
    /// The most rows a data query may read to filter or sort on a calculated field, which only exists after the rows are read
    /// (FDW-1045). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required, and resolved like <see cref="MaxPaginationSize"/> on every data query, so a missing setting fails the first
    /// query and not only the ones that name a calculated field. A query that would read more is refused, never cut short.
    /// </remarks>
    public const string MaxCalculatedFilterRows = "MaxCalculatedFilterRows";

    /// <summary>
    /// The most results one calculation array may define (FDW-1209). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: a run that applies a Built transformation reads it when it starts, and a missing or non-positive value fails
    /// the run naming the setting; no limit is assumed.
    /// </remarks>
    public const string MaxCalculationResults = "MaxCalculationResults";

    /// <summary>
    /// The most nodes one calculation array may have across all its formulas (FDW-1209). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>Required, and resolved like <see cref="MaxCalculationResults"/>.</remarks>
    public const string MaxCalculationNodes = "MaxCalculationNodes";

    /// <summary>
    /// The deepest nesting one formula of a calculation array may have (FDW-1209). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>Required, and resolved like <see cref="MaxCalculationResults"/>.</remarks>
    public const string MaxCalculationDepth = "MaxCalculationDepth";

    /// <summary>
    /// The most rows one table output of a calculation set may hold (FDW-1231). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>Required, and resolved like <see cref="MaxCalculationResults"/>.</remarks>
    public const string MaxSetOutputRows = "MaxSetOutputRows";

    /// <summary>
    /// The most rows a lookup table of a calculation set may hold (FDW-1231). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>Required, and resolved like <see cref="MaxCalculationResults"/>.</remarks>
    public const string MaxSetLookupRows = "MaxSetLookupRows";

    /// <summary>
    /// The secret manager connection credentials are stored in (FDW-1086).
    /// DataType: String. Required to store a connection's credentials.
    /// </summary>
    /// <remarks>
    /// A server setting the platform states: when it is empty, storing a connection's credentials fails rather than
    /// guessing a secret manager. Callers never name one; the server writes each credential here.
    /// </remarks>
    public const string ConnectionCredentialSecretManager = "ConnectionCredentialSecretManager";

    /// <summary>
    /// How long a set-password link works after an administrator creates it, in minutes (FDW-941).
    /// DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>Required: creating a link fails when it resolves to nothing positive.</remarks>
    public const string PasswordLinkLifetimeMinutes = "PasswordLinkLifetimeMinutes";

    /// <summary>
    /// The console page a set-password link opens (FDW-941); the token follows as <c>#token=</c>.
    /// DataType: String. Required.
    /// </summary>
    /// <remarks>
    /// Per environment: it names the console that serves this API. The token goes in the fragment so it
    /// never reaches a server log or a Referer header.
    /// </remarks>
    public const string PasswordLinkUrl = "PasswordLinkUrl";

    /// <summary>
    /// The notification channel a set-password link is emailed through (FDW-941).
    /// DataType: String. Required to email a link.
    /// </summary>
    /// <remarks>
    /// It names an email channel, because the recipient is the user's email address. Without it a link is
    /// still created and returned to share; only the email is refused.
    /// </remarks>
    public const string PasswordLinkNotificationChannel = "PasswordLinkNotificationChannel";

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

    /// <summary>
    /// The longest a server-side session lives from sign-in, in hours, however active it stays (FDW-903).
    /// </summary>
    public const string SessionAbsoluteLifetimeHours = "SessionAbsoluteLifetimeHours";

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
            [MaxPaginationSize] = new("Int32", "The largest page a paginated list endpoint returns."),
            [DefaultPaginationSize] = new("Int32", "The page a paginated list endpoint returns when the request names no take."),
            [ApiKeyMaxPaginationSize] = new("Int32", "The largest page a caller authenticated with an API key may ask for; 0 means no limit."),
            [ApiKeyDefaultPaginationSize] = new("Int32", "The page a caller authenticated with an API key gets when the request names no take; 0 means no limit, every row is returned."),
            [MaxCalculatedFilterRows] = new("Int32", "The most rows a data query may read to filter or sort on a calculated field in memory."),
            [MaxCalculationResults] = new("Int32", "The most results one calculation array may define."),
            [MaxCalculationNodes] = new("Int32", "The most nodes one calculation array may have across all its formulas."),
            [MaxCalculationDepth] = new("Int32", "The deepest nesting one formula of a calculation array may have."),
            [MaxSetOutputRows] = new("Int32", "The most rows one table output of a calculation set may hold."),
            [MaxSetLookupRows] = new("Int32", "The most rows a lookup table of a calculation set may hold."),
            [ConnectionCredentialSecretManager] = new("String", "The secret manager connection credentials are stored in."),
            [PasswordLinkLifetimeMinutes] = new("Int32", "How long a set-password link works after an administrator creates it, in minutes."),
            [PasswordLinkUrl] = new("String", "The console page a set-password link opens; the token follows as #token=."),
            [PasswordLinkNotificationChannel] = new("String", "The notification channel a set-password link is emailed through."),
            [MaxConcurrentQueries] = new("Int32", "Maximum number of concurrent queries allowed per tenant."),
            [MaxPreviewRows] = new("Int32", "Maximum number of rows returned by data preview endpoints."),
            [DefaultTimeoutMs] = new("Int32", "Default timeout in milliseconds for data operations."),
            [EnableLineageTracking] = new("Boolean", "Whether data lineage tracking is enabled."),
            [MaxRetryAttempts] = new("Int32", "Maximum number of retry attempts for transient failures."),
            [SystemName] = new("String", "Human-readable system / instance name shown in the management UI."),
            [Timezone] = new("String", "Default display timezone for the management UI."),
            [DateFormat] = new("String", "Default date-format pattern for the management UI."),
            [SessionTimeoutMinutes] = new("Int32", "Idle session timeout, in minutes."),
            [SessionAbsoluteLifetimeHours] = new("Int32", "Longest a server-side session lives from sign-in, in hours, however active it stays."),
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
