namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Host selector for the health monitor domain: names WHICH <c>settings.HealthMonitor</c> row this
/// host uses. Selector only — all monitor configuration (implementation choice via
/// <c>ServiceOptionType</c>, intervals, retention) lives on the ConfigurationDb row.
/// </summary>
/// <remarks>
/// Same species as <c>Users:CredentialServiceName</c> / <c>CredentialsSqlOptions</c>: rows are shared
/// in ConfigurationDb; which row applies is per-host appsettings. A missing/blank <see cref="Name"/>
/// fails loud at first health query (NO FALLBACKS) — there is no default row.
/// </remarks>
public sealed class HealthMonitorSelectionOptions
{
    /// <summary>The appsettings section this selector binds from.</summary>
    public const string SectionName = "HealthMonitor";

    /// <summary>Gets or sets the name of the ConfigurationDb health monitor row this host uses.</summary>
    public string Name { get; set; } = string.Empty;
}
