using System;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Builders;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.DataStores;

/// <summary>
/// The single per-transport <see cref="IDataStoreBuilder"/> selector available to the UI. Always
/// resolves to <see cref="GenericDataStoreBuilder"/> — the UI has no transport-type registry
/// (<c>DataStoreTypes</c>/<c>ServiceTypeCollection</c> lives in the excluded core connections
/// package that <c>Fdw.Data.DataNodes</c> deliberately stays free of; see
/// <see cref="IDataStoreBuilderSelector"/>'s own remarks).
/// </summary>
/// <remarks>
/// Why: mirrors the server-side <c>DataStoreTypesBuilderSelector</c> (which dispatches to a real
/// transport-specific builder via <c>DataStoreTypes.ByName(...)</c>) but the UI has exactly one
/// builder for every store's Format+row-shaping composition (<c>GenericDataStoreBuilder</c>) since
/// it never talks to a physical connection directly — all reads go through the .Clients API surface.
/// </remarks>
public sealed class GenericBuilderSelector : IDataStoreBuilderSelector
{
    /// <inheritdoc/>
    public IGenericResult<IDataStoreBuilder> Select(DataStoreConfiguration configuration, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Why: the UI has no transport registry to resolve a declared default response format from,
        // and the display DTOs carry no format hint (DataStoreDetailPayload.StoreType is a connection/store
        // discriminator like "SqlServer", not a serialization format) — FormatTypes.NotFound is passed
        // as the default so a container without its own explicit Format
        // (ContainerComposition.ResolveFormat) fails loud at read time rather than guessing Json/Tabular.
        // A container's own Format (when the API starts exposing it — see the gap block in
        // ClientsDataStoreConfigurationProvider) still takes priority over this default.
        return GenericResult<IDataStoreBuilder>.Success(new GenericDataStoreBuilder(FormatTypes.NotFound, logger));
    }
}
