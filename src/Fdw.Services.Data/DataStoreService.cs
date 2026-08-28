using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Serves the datastores configuration describes.
/// </summary>
/// <remarks>
/// Each <c>Get</c> reads the configuration for what was asked for and hands it to a builder.
/// Choosing the configuration is this service's job; turning it into a datastore is the builder's.
/// </remarks>
public sealed class DataStoreService : IDataStoreProvider
{
    private readonly DataStoreConfigurationProvider _configuration;
    private readonly Func<IDataStoreBuilder> _builder;
    private readonly IDataStoreProvider _nodes;
    private readonly ILogger<DataStoreService> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataStoreService"/> class.</summary>
    /// <param name="configuration">Supplies datastore configuration.</param>
    /// <param name="builder">Produces a builder per datastore built.</param>
    /// <param name="nodes">Answers path and container reads inside a built datastore.</param>
    /// <param name="logger">The logger.</param>
    public DataStoreService(
        DataStoreConfigurationProvider configuration,
        Func<IDataStoreBuilder> builder,
        IDataStoreProvider nodes,
        ILogger<DataStoreService>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        _logger = logger ?? NullLogger<DataStoreService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataStore>> Get(string name, CancellationToken cancellationToken = default)
        => await Build(await _configuration.Get(name, cancellationToken).ConfigureAwait(false), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult<IDataStore>> Get(Guid id, CancellationToken cancellationToken = default)
        => await Build(await _configuration.Get(id, cancellationToken).ConfigureAwait(false), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IDataStore>>> Get(CancellationToken cancellationToken = default)
    {
        var configurations = await _configuration.Get(cancellationToken).ConfigureAwait(false);
        if (configurations.IsFailure)
            return configurations.ToNewResult<IReadOnlyList<IDataStore>>();

        var stores = new List<IDataStore>();
        foreach (var configuration in configurations.Value ?? [])
        {
            var built = await Build(GenericResult<DataStoreConfiguration>.Success(configuration), cancellationToken)
                .ConfigureAwait(false);
            if (built.IsFailure)
                return built.ToNewResult<IReadOnlyList<IDataStore>>();

            stores.Add(built.Value!);
        }

        return GenericResult<IReadOnlyList<IDataStore>>.Success(stores);
    }

    /// <inheritdoc />
    public Task<IGenericResult<IDataNodePath>> Get(string dataStoreName, string pathName, CancellationToken cancellationToken = default)
        => _nodes.Get(dataStoreName, pathName, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IDataContainer>> Get(string dataStoreName, string pathName, string containerName, CancellationToken cancellationToken = default)
        => _nodes.Get(dataStoreName, pathName, containerName, cancellationToken);

    private async Task<IGenericResult<IDataStore>> Build(
        IGenericResult<DataStoreConfiguration> configuration, CancellationToken cancellationToken)
    {
        if (configuration.IsFailure)
            return configuration.ToNewResult<IDataStore>();

        var builder = _builder();
        var configured = builder.Configure(configuration.Value!);
        return configured.IsFailure
            ? configured.ToNewResult<IDataStore>()
            : await builder.Build(cancellationToken).ConfigureAwait(false);
    }
}
