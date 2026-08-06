using Fdw.Collections;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.Clients.Abstractions.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.DataStores;

/// <summary>
/// ServiceTypeOption that wires the UI's .Clients-fetched <see cref="IDataStoreProvider"/> —
/// <see cref="ConfiguredDataStoreProvider"/> (<c>Fdw.Data.DataNodes</c>, gateway-free tree composition)
/// fed by <see cref="ClientsDataStoreConfigurationProvider"/> (reads over the already-registered
/// <c>DataStoreApiClient</c>) and <see cref="GenericBuilderSelector"/> (the UI's single builder).
/// </summary>
/// <remarks>
/// Why: mirrors <c>SchemaClientType</c>/<c>ScheduleClientType</c> — an <c>ApiClientTypeBase&lt;TClient&gt;</c>
/// option whose <c>Register</c> phase registers a provider interface beyond its own TClient.
/// <see cref="IDataStoreProvider"/> is used as TClient purely for <c>ServiceTypeBase.Id</c> uniqueness
/// (it is also what gets registered). No HTTP client of its own is configured here — this option
/// depends on <c>DataStoreClientType</c>'s already-registered <c>DataStoreApiClient</c>, resolved
/// lazily through DI when <see cref="ApiClientTypes"/> iterates every option's
/// <c>Register</c> — so <c>Configure</c> is left as the <c>ApiClientTypeBase</c> no-op.
/// TryAdd semantics: server hosts never load this UI package, so there is no gateway-backed
/// <see cref="IDataStoreProvider"/> to protect against here, but TryAdd is still the safe idiom that
/// matches every other option in this collection.
/// </remarks>
// TClient uniquely identifies this option — ServiceTypeBase.Id is computed from typeof(TService).FullName + typeof(TFactory).FullName
[ServiceTypeOption(typeof(ApiClientTypes), "DataStoreProviderClient")]
public sealed class DataStoreProviderClientType : ApiClientTypeBase<IDataStoreProvider>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreProviderClientType"/> class.
    /// </summary>
    public DataStoreProviderClientType() : base("DataStoreProviderClient", "DataStore Provider (Clients-backed)") {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.TryAddScoped<IServiceConfigurationProvider<DataStoreConfiguration>, ClientsDataStoreConfigurationProvider>();
            builder.Services.TryAddScoped<IDataStoreBuilderSelector, GenericBuilderSelector>();
            builder.Services.TryAddScoped<IDataStoreProvider>(sp => new ConfiguredDataStoreProvider(
                sp.GetService<ILogger<ConfiguredDataStoreProvider>>(),
                sp.GetRequiredService<IServiceConfigurationProvider<DataStoreConfiguration>>(),
                sp.GetRequiredService<IDataStoreBuilderSelector>()));
            return builder;
        });
 }

}
