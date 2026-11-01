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
    /// The most pipeline runs in flight on the whole ETL host at once (FDW-3085). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: a run fails to start when the setting is absent or not positive, and no limit is assumed. It is read at server level; a
    /// tenant's own cap is <see cref="MaxConcurrentPipelineRunsPerTenant"/>.
    /// </remarks>
    public const string MaxConcurrentPipelineRunsPerServer = "MaxConcurrentPipelineRunsPerServer";

    /// <summary>
    /// The most pipeline runs in flight for one tenant at once (FDW-3085). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required, and resolved server, then tenant, so a tenant can raise or lower it for itself. A tenant never gets more runs in flight
    /// than <see cref="MaxConcurrentPipelineRunsPerServer"/> allows the host.
    /// </remarks>
    public const string MaxConcurrentPipelineRunsPerTenant = "MaxConcurrentPipelineRunsPerTenant";

    /// <summary>
    /// The most pipeline triggers the ETL host holds before it starts them (FDW-3155). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: the host does not start when the setting is absent or not positive, and no number is assumed. It is read once, at host
    /// start, for the host; a change takes effect at the next start. A trigger that arrives while the host holds that many is dropped.
    /// </remarks>
    public const string MaxQueuedPipelineTriggers = "MaxQueuedPipelineTriggers";

    /// <summary>
    /// How many times the ETL host starts the same pipeline run again after the host stopped while it ran (FDW-3236). DataType: Int32. Zero or more.
    /// </summary>
    /// <remarks>
    /// Required, with no number assumed: while the setting is not set, the host starts no cut-off run again. It closes each as failed, naming the setting.
    /// Read when the host starts. Zero is a number someone sets and means a cut-off run is never started again.
    /// </remarks>
    public const string MaxPipelineRunRestarts = "MaxPipelineRunRestarts";

    /// <summary>
    /// How often a host looks at its configuration databases to see whether one was dropped and created again, in seconds (FDW-3207).
    /// DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: a host that registers the settings does not start when the setting is absent or not positive, and no number is assumed.
    /// It is read once, at host start, for the host; a change takes effect at the next start. When a look finds a database reset or
    /// out of reach, the host empties its result cache.
    /// </remarks>
    public const string ResetWatchIntervalSeconds = "ResetWatchIntervalSeconds";

    /// <summary>
    /// The command timeout, in seconds, of a connection or secret manager that states none of its own (FDW-3284).
    /// DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required, with no number assumed in code or in a column: a connection or secret manager that states no command timeout and finds no
    /// positive row is not built, and the failure names it and this setting. A connection that states its own timeout never reads it.
    /// The connections a host needs before it can read its settings (the platform configuration and the auth database in
    /// <c>configurationSchema.json</c>) state their own.
    /// </remarks>
    public const string DefaultCommandTimeoutSeconds = "DefaultCommandTimeoutSeconds";

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
            [MaxConcurrentPipelineRunsPerServer] = new("Int32", "The most pipeline runs in flight on the whole ETL host at once."),
            [MaxConcurrentPipelineRunsPerTenant] = new("Int32", "The most pipeline runs in flight for one tenant at once; a tenant may override the server's value."),
            [MaxQueuedPipelineTriggers] = new("Int32", "The most pipeline triggers the ETL host holds before it starts them; a trigger past that is dropped. Read at host start."),
            [MaxPipelineRunRestarts] = new("Int32", "How many times the ETL host starts the same pipeline run again after the host stopped while it ran. Not set: a cut-off run is closed as failed and not started again. Read at host start."),
            [ResetWatchIntervalSeconds] = new("Int32", "How often, in seconds, a host looks at its configuration databases to see whether one was reset; the host then empties its result cache. Read at host start."),
            [DefaultCommandTimeoutSeconds] = new("Int32", "The command timeout, in seconds, of a connection or secret manager that states none of its own. Not set: such a connection is not built."),
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
    /// <remarks>
    /// The registry is the settings listed above and the rate limit settings of every policy in
    /// <c>RateLimitPolicies</c> (<c>RateLimit.{policy}.{field}</c>, FDW-1945), which are made from that collection rather than
    /// listed.
    /// </remarks>
    public static bool TryGet(string name, out SettingDefinition definition)
        => KnownSettings.TryGetValue(name, out definition!) || RateLimitSettingDefinitions.TryGet(name, out definition);
}
