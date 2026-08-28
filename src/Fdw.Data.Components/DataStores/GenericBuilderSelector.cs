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

        return GenericResult<IDataStoreBuilder>.Success(new GenericDataStoreBuilder(FormatTypes.NotFound, logger));
    }
}
