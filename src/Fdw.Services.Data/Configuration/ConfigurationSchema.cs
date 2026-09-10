using System.Collections.Generic;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// Root POCO for the <c>configurationSchema.json</c> file shipped with every entry-point app.
/// Bound via <c>IOptionsMonitor&lt;ConfigurationSchema&gt;</c> from the "ConfigurationSchema" section.
/// </summary>
/// <remarks>
/// The three child lists mirror the top-level JSON keys under the "ConfigurationSchema" wrapper.
/// <c>DataStores</c> carries the full nested graph (Paths → Containers → Fields + Keys) because the
/// existing <see cref="DataStoreImplementationConfiguration"/> already declares nested <see cref="DataStoreImplementationConfiguration.Paths"/>
/// using <c>List&lt;DataPathConfiguration&gt;</c>, which in turn nests Containers → Fields + Keys.
/// No parallel POCO hierarchy is needed — IOptions binding uses the same types the rest of FDW uses.
/// </remarks>
public sealed class ConfigurationSchema
{
    /// <summary>
    /// Gets or sets the connections available to the entry-point app.
    /// Corresponds to the <c>ConfigurationSchema:Connections</c> configuration section.
    /// </summary>
#pragma warning disable MA0016 // Prefer collection abstraction — required for IOptions binding
    public IList<IConnectionImplementationConfiguration> Connections { get; set; } = new List<IConnectionImplementationConfiguration>();


    /// <summary>
    /// Gets or sets the data stores (with their full Paths → Containers → Fields + Keys hierarchy).
    /// Corresponds to the <c>ConfigurationSchema:DataStores</c> configuration section.
    /// </summary>
    public IList<DataStoreImplementationConfiguration> DataStores { get; set; } = new List<DataStoreImplementationConfiguration>();

#pragma warning restore MA0016

    /// <summary>
    /// Gets or sets the <c>Implementation</c> name of the single Multitenancy option (e.g.
    /// "SingleTenant", "Sql") this host runs. Corresponds to the
    /// <c>ConfigurationSchema:Multitenancy</c> configuration key.
    /// </summary>
    /// <remarks>
    /// Which Multitenancy option a host runs is per-host topology — declared once here, alongside
    /// <see cref="Connections"/>/<see cref="DataStores"/>, not a shared
    /// ConfigurationDb row. <c>Fdw.Services.Multitenancy.MultitenancyTypes.Configure{TBuilder}</c> reads
    /// this value to resolve and drive exactly one option (NO FALLBACKS: null/whitespace or an
    /// unrecognized value is a startup failure, not a silent default).
    /// </remarks>
    public string? Multitenancy { get; set; }
}
