using System;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Resolves the per-transport <see cref="IDataStoreBuilder"/> for a store configuration via the
/// registered <see cref="DataStoreTypes"/> TypeCollection.
/// </summary>
/// <remarks>
/// Why: <see cref="ConfiguredDataStoreProvider"/> (the pure, connection-agnostic core that lives in
/// <c>Fdw.Data.DataNodes</c>) cannot reference <see cref="DataStoreTypes"/> directly — that
/// TypeCollection lives alongside the connection/service-type packages the core deliberately excludes
/// (see the "Why" remarks on <see cref="IDataStoreBuilderSelector"/>). This is the concrete,
/// connection-aware selector that <see cref="ConfigurationGatewayDataStoreProvider.Register"/> registers
/// so the core provider can dispatch to a transport builder without knowing which transports exist.
/// </remarks>
public sealed class DataStoreTypesBuilderSelector : IDataStoreBuilderSelector
{
    /// <inheritdoc/>
    public IGenericResult<IDataStoreBuilder> Select(DataStoreConfiguration configuration, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var option = DataStoreTypes.ByName(configuration.ServiceOptionType);
        if (option == DataStoreTypes.NotFound)
        {
            return GenericResult<IDataStoreBuilder>.Failure(
                DataStoreProviderLog.NoDataStoreTypeFoundAtStartup(
                    logger ?? NullLogger.Instance,
                    configuration.ServiceOptionType ?? "(null)",
                    configuration.Name));
        }

        return GenericResult<IDataStoreBuilder>.Success(option.SupplyBuilder(logger));
    }
}
