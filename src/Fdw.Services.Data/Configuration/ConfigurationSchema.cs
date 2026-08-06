using System.Collections.Generic;
using Fdw.Aegis.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.SecretManagers;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// Root POCO for the <c>configurationSchema.json</c> file shipped with every entry-point app.
/// Bound via <c>IOptionsMonitor&lt;ConfigurationSchema&gt;</c> from the "ConfigurationSchema" section.
/// </summary>
/// <remarks>
/// The three child lists mirror the top-level JSON keys under the "ConfigurationSchema" wrapper.
/// <c>DataStores</c> carries the full nested graph (Paths → Containers → Fields + Keys) because the
/// existing <see cref="DataStoreConfiguration"/> already declares nested <see cref="DataStoreConfiguration.Paths"/>
/// using <c>List&lt;DataPathConfiguration&gt;</c>, which in turn nests Containers → Fields + Keys.
/// No parallel POCO hierarchy is needed — IOptions binding uses the same types the rest of FDW uses.
/// </remarks>
public sealed class ConfigurationSchema
{
    /// <summary>
    /// Gets or sets the connections available to the entry-point app.
    /// Corresponds to the <c>ConfigurationSchema:Connections</c> configuration section.
    /// </summary>
    // Why: List<T> is required for IOptions binding — the configuration system instantiates
    // concrete collection types. IList<T> at the property level satisfies both the binding
    // contract and the MA0016 "prefer abstraction" rule.
#pragma warning disable MA0016 // Prefer collection abstraction — required for IOptions binding
    public IList<ConnectionConfiguration> Connections { get; set; } = new List<ConnectionConfiguration>();

    /// <summary>
    /// Gets or sets the secret managers available to the entry-point app.
    /// Corresponds to the <c>ConfigurationSchema:SecretManagers</c> configuration section.
    /// </summary>
    public IList<SecretManagerConfiguration> SecretManagers { get; set; } = new List<SecretManagerConfiguration>();

    /// <summary>
    /// Gets or sets the data stores (with their full Paths → Containers → Fields + Keys hierarchy).
    /// Corresponds to the <c>ConfigurationSchema:DataStores</c> configuration section.
    /// </summary>
    public IList<DataStoreConfiguration> DataStores { get; set; } = new List<DataStoreConfiguration>();

    /// <summary>
    /// Gets or sets the Aegis Gateway commands declared for this entry-point app.
    /// Corresponds to the <c>ConfigurationSchema:Commands</c> configuration section.
    /// </summary>
    /// <remarks>
    /// Why here rather than in the net10 <c>Fdw.Aegis</c> package (G2): <c>AegisCommandConfiguration</c>
    /// lives in <c>Fdw.Aegis.Configuration</c>, a package this project can reference without a cycle —
    /// mirrors how <see cref="ConnectionConfiguration"/> sits in <c>Fdw.Services.Connections</c> rather
    /// than a hypothetical package that itself depends on <c>Fdw.Services.Data</c>.
    /// </remarks>
    public IList<AegisCommandConfiguration> Commands { get; set; } = new List<AegisCommandConfiguration>();
#pragma warning restore MA0016

    /// <summary>
    /// Gets or sets the <c>ServiceOptionType</c> name of the single Multitenancy option (e.g.
    /// "SingleTenant", "Sql") this host runs. Corresponds to the
    /// <c>ConfigurationSchema:Multitenancy</c> configuration key.
    /// </summary>
    /// <remarks>
    /// Which Multitenancy option a host runs is per-host topology — declared once here, alongside
    /// <see cref="Connections"/>/<see cref="SecretManagers"/>/<see cref="DataStores"/>, not a shared
    /// ConfigurationDb row. <c>Fdw.Services.Multitenancy.MultitenancyTypes.Configure{TBuilder}</c> reads
    /// this value to resolve and drive exactly one option (NO FALLBACKS: null/whitespace or an
    /// unrecognized value is a startup failure, not a silent default).
    /// </remarks>
    public string? Multitenancy { get; set; }
}
