using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Selects the per-transport <see cref="IDataStoreBuilder"/> for a given store configuration.
/// </summary>
/// <remarks>
/// Why: <c>Fdw.Data.DataNodes</c> must stay connection-agnostic — it cannot reference
/// <c>ServiceTypeCollection</c>/<c>DataStoreTypes</c> (those live in the core connections/service-type
/// packages this package excludes). The transport-to-builder dispatch that used to happen inline via
/// <c>DataStoreTypes.ByName(store.ServiceOptionType).SupplyBuilder(...)</c> is now expressed behind this
/// interface, so a caller outside this package (which CAN reference <c>DataStoreTypes</c>) supplies the
/// implementation while the pure, gateway-free <see cref="ConfiguredDataStoreProvider"/> only depends on
/// the abstraction.
/// </remarks>
public interface IDataStoreBuilderSelector
{
    /// <summary>
    /// Selects the <see cref="IDataStoreBuilder"/> for the given store configuration.
    /// </summary>
    /// <param name="configuration">The store configuration whose transport determines the builder.</param>
    /// <param name="logger">Optional logger passed through to the selected builder for build diagnostics.</param>
    /// <returns>
    /// Success with the resolved builder, or Failure (with MessageLogging) when no builder is registered
    /// for the configuration's transport.
    /// </returns>
    IGenericResult<IDataStoreBuilder> Select(DataStoreConfiguration configuration, ILogger? logger = null);
}
