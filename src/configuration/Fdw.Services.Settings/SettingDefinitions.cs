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
    /// How often the console reads the run queue again while the queue tab is open, in seconds (FDW-3382). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required, with no number assumed: while the setting is not set, or is not positive, the run queue answers no refresh interval and the tab does not read again
    /// by itself (its Read again button still works). A tenant may set its own value; the answer carries the value that applies to the caller.
    /// </remarks>
    public const string LiveViewRefreshSeconds = "LiveViewRefreshSeconds";

    /// <summary>
    /// The most pipelines one line of the lineage groups names (FDW-3824). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: <c>GET /lineage/groups</c> fails when it resolves to nothing positive, so an absent setting never reads as no limit. A line
    /// always says how many pipelines run along it; this only limits how many it names. It resolves server, then tenant, so a tenant can raise or lower it.
    /// </remarks>
    public const string LineageLinePipelines = "LineageLinePipelines";

    /// <summary>
    /// How many hours a completed run of a pipeline that writes a data set counts as recent (FDW-4488). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required, with no number assumed: the status of a data set in a list fails (DATASETS-90003) when it resolves to nothing positive. A data set whose writers have
    /// completed a run inside the window is Current; one with writers and no completed run inside it is Idle. It resolves server, then tenant, so a tenant can set its own.
    /// </remarks>
    public const string DataSetStatusWindowHours = "DataSetStatusWindowHours";

    /// <summary>
    /// Whether a host that serves no real-time hub relays its pipeline events to the host that does (FDW-3773). DataType: Boolean.
    /// </summary>
    /// <remarks>
    /// Required on a host that serves no hub: an absent value is logged as a missing setting and the host publishes nothing, and no
    /// value is assumed. False means the host publishes no pipeline events and says so once. A host that serves the hub broadcasts
    /// through SignalR and does not read it. It is read each time an event is sent, so a change takes effect without a restart.
    /// </remarks>
    public const string PipelineEventRelayEnabled = "PipelineEventRelayEnabled";

    /// <summary>
    /// Whether an administrator of one tenant may enable a disabled connection that belongs to another tenant when they can see it (FDW-4459). DataType: Boolean.
    /// </summary>
    /// <remarks>
    /// A server row states it for the server. Where there is none, the value <c>CrossTenantChangeOption</c> declares applies (true), which a host overrides at registration.
    /// A refused enable answers a failure that names this setting.
    /// </remarks>
    public const string CrossTenantConnectionEnable = Fdw.Services.Multitenancy.Abstractions.CrossTenantChangeOption.ConnectionEnable;

    /// <summary>
    /// Whether an administrator of one tenant may change a data store that belongs to another tenant when they can see it (FDW-4459). DataType: Boolean.
    /// </summary>
    /// <remarks>
    /// A server row states it for the server. Where there is none, the value <c>CrossTenantChangeOption</c> declares applies (false), which a host overrides at registration.
    /// A refused write answers a failure that names this setting.
    /// </remarks>
    public const string CrossTenantDataStoreWrite = Fdw.Services.Multitenancy.Abstractions.CrossTenantChangeOption.DataStoreWrite;

    /// <summary>
    /// Whether founding a tenant or an organization makes a default workspace (FDW-4599). DataType: Boolean.
    /// </summary>
    /// <remarks>
    /// A server row states it. Where there is none, the value <c>WorkspaceFoundingOption</c> declares applies (true: founding makes one, as it always did), which a host overrides at
    /// registration. Off, a tenant is founded with no workspace and a new data store, data set or pipeline is placed in none.
    /// </remarks>
    public const string FoundingMakesDefaultWorkspace = Fdw.Services.Multitenancy.Abstractions.WorkspaceFoundingOption.FoundingMakesDefaultWorkspace;

    /// <summary>
    /// How often, in seconds, the ETL host publishes the components of the runs in flight (FDW-3778). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: when the setting is absent or not positive the host logs it once and publishes no components, and no number is assumed.
    /// It is read once, at host start; a change takes effect at the next start. A run's list is sent only when it changed since the last
    /// one sent.
    /// </remarks>
    public const string PipelineComponentsPublishSeconds = "PipelineComponentsPublishSeconds";

    /// <summary>
    /// How often, in seconds, the console reads the escalation alerts again while the alerts page is open (FDW-3863). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Not set, or not positive for the caller's scope: the list answers no interval and the page does not read again by itself. No number is assumed.
    /// </remarks>
    public const string EscalationAlertsRefreshSeconds = "EscalationAlertsRefreshSeconds";

    /// <summary>
    /// How often, in seconds, a host looks at its open escalation alerts and sends the levels that have fallen due (FDW-3863). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required on a host that runs the escalation timer: it is read when the host starts, and a value that is absent or not positive keeps the timer from starting and says so,
    /// with no number assumed.
    /// </remarks>
    public const string EscalationAdvanceIntervalSeconds = "EscalationAdvanceIntervalSeconds";

    /// <summary>
    /// The notification priority an escalation level is sent with, the name of a notification priority such as Normal, High or Critical (FDW-3863). DataType: String.
    /// </summary>
    /// <remarks>
    /// Required where a level is sent: a value that is not set or names no priority keeps the level from being sent, says so naming the setting, and is tried again on the next pass.
    /// No priority is assumed. It is read for the alert's tenant.
    /// </remarks>
    public const string EscalationNotificationPriority = "EscalationNotificationPriority";

    /// <summary>
    /// How many times the ETL host starts the same pipeline run again after the host stopped while it ran (FDW-3236). DataType: Int32. Zero or more.
    /// </summary>
    /// <remarks>
    /// Required, with no number assumed: while the setting is not set, the host starts no cut-off run again. It closes each, and the records under it, as Interrupted, naming the setting (FDW-3787).
    /// Read when the host starts. Zero is a number someone sets and means a cut-off run is never started again.
    /// </remarks>
    public const string MaxPipelineRunRestarts = "MaxPipelineRunRestarts";

    /// <summary>
    /// How long the ETL host remembers the key of a scheduled fire, in seconds (FDW-4317). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required, with no number assumed: while the setting is absent or not positive, a scheduled fire is refused naming it. A repeat of a fire within that time answers the run the
    /// first call started; a fire whose time is older than it is refused as expired. It must be longer than the scheduler's longest retry of one fire.
    /// </remarks>
    public const string PipelineTriggerKeyRetentionSeconds = "PipelineTriggerKeyRetentionSeconds";

    /// <summary>
    /// How long a stopping ETL host lets the pipeline runs in flight go on before it stops them, in seconds (FDW-3786). DataType: Int32. Must be positive.
    /// </summary>
    /// <remarks>
    /// Required: the host does not start when the setting is absent or not positive, and no number is assumed. It is read once, at host start; a change
    /// takes effect at the next start. When the host is told to stop it takes no new runs, waits up to this time for the runs in flight to finish, then
    /// stops the rest and marks each Interrupted. The host's shutdown timeout is twice this time (the time to wait and as long again to close the records),
    /// and the service unit's stop timeout must be longer than that.
    /// </remarks>
    public const string PipelineStopGraceSeconds = "PipelineStopGraceSeconds";

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
    /// The connect timeout, in seconds, of a connection that states none of its own (FDW-3580).
    /// </summary>
    /// <remarks>
    /// Required, with no number assumed in code or in a column: a connection that states no connect timeout and finds no positive row is not
    /// built, and the failure names it and this setting. A connection that states its own timeout never reads it. The connections a host needs
    /// before it can read its settings (the platform configuration and the auth database in <c>configurationSchema.json</c>) state their own.
    /// </remarks>
    public const string DefaultConnectionTimeoutSeconds = "DefaultConnectionTimeoutSeconds";

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
            [LineageLinePipelines] = new("Int32", "The most pipelines one line of the lineage groups names; the line still counts all of them."),
            [DataSetStatusWindowHours] = new("Int32", "How many hours a completed run of a pipeline that writes a data set counts as recent: inside it the data set is Current, outside it Idle. Not set: the data set list stops and names this setting."),
            [LiveViewRefreshSeconds] = new("Int32", "How often, in seconds, the console reads the run queue again while the queue tab is open. Not set: the tab does not read again by itself."),
            [PipelineEventRelayEnabled] = new("Boolean", "Whether a host that serves no real-time hub relays its pipeline events to the host that does."),
            [CrossTenantConnectionEnable] = new("Boolean", "Whether an administrator of one tenant may enable a disabled connection that belongs to another tenant when they can see it. Not set: the value the host or the service type declares applies."),
            [FoundingMakesDefaultWorkspace] = new("Boolean", "Whether founding a tenant or an organization makes a default workspace. Not set: the value the host or the service type declares applies (true)."),
            [CrossTenantDataStoreWrite] = new("Boolean", "Whether an administrator of one tenant may change a data store that belongs to another tenant when they can see it. Not set: the value the host or the service type declares applies."),
            [EscalationAlertsRefreshSeconds] = new("Int32", "How often, in seconds, the console reads the escalation alerts again while the alerts page is open. Not set: the page does not read again by itself."),
            [EscalationAdvanceIntervalSeconds] = new("Int32", "How often, in seconds, the host looks at its open escalation alerts and sends the levels that have fallen due. Not set: the host does not start the escalation timer and says so."),
            [EscalationNotificationPriority] = new("String", "The notification priority an escalation level is sent with: the name of a notification priority such as Normal, High or Critical. Not set, or naming no priority: the level is not sent, and the next pass tries again."),
            [PipelineStopGraceSeconds] = new("Int32", "How long, in seconds, a stopping ETL host lets the pipeline runs in flight go on before it stops them and marks each Interrupted. Not set or not positive: the host does not start. Read at host start. The service unit's stop timeout must be longer than twice this."),
            [PipelineTriggerKeyRetentionSeconds] = new("Int32", "How long the ETL host remembers the key of a scheduled fire, in seconds. A retry of the same fire within that time gets the run the first call started instead of a second run; a fire older than it is refused as expired. Required: a scheduled fire is refused while it is not set."),
            [MaxPipelineRunRestarts] = new("Int32", "How many times the ETL host starts the same pipeline run again after the host stopped while it ran. Not set: a cut-off run is left Interrupted and not started again; 0 does the same. Read at host start."),
            [PipelineComponentsPublishSeconds] = new("Int32", "How often, in seconds, the ETL host publishes the components of the runs in flight; read at host start."),
            [ResetWatchIntervalSeconds] = new("Int32", "How often, in seconds, a host looks at its configuration databases to see whether one was reset; the host then empties its result cache. Read at host start."),
            [DefaultCommandTimeoutSeconds] = new("Int32", "The command timeout, in seconds, of a connection or secret manager that states none of its own. Not set: such a connection is not built."),
            [DefaultConnectionTimeoutSeconds] = new("Int32", "The connect timeout, in seconds, of a connection that states none of its own. Not set: such a connection is not built."),
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
        => KnownSettings.TryGetValue(name, out definition!) || RateLimitSettingDefinitions.TryGet(name, out definition) ||
           DefaultValueSettings.TryGet(name, out definition);
}
