using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Serves the datasets configuration describes.
/// </summary>
/// <remarks>
/// The mirror of <see cref="DataStoreService"/>, and deliberately a separate service: both kinds
/// answer to <c>Get(name)</c>, so one class serving both would have to rename a member to hold them
/// apart. Two services keep the convention and each stays the shape every other domain service has.
/// </remarks>
public sealed class DataSetService : IDataSetProvider
{
    private readonly IDataSetConfigurationProvider _configuration;
    private readonly Func<IDataSetBuilder> _builder;
    private readonly ILogger<DataSetService> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetService"/> class.</summary>
    /// <param name="configuration">Supplies dataset configuration.</param>
    /// <param name="builder">Produces a builder per dataset built.</param>
    /// <param name="logger">The logger.</param>
    public DataSetService(
        IDataSetConfigurationProvider configuration,
        Func<IDataSetBuilder> builder,
        ILogger<DataSetService>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _logger = logger ?? NullLogger<DataSetService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataSet>> Get(string name, CancellationToken cancellationToken = default)
        => await Build(await _configuration.Get(name, cancellationToken).ConfigureAwait(false), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult<IDataSet>> Get(Guid id, CancellationToken cancellationToken = default)
        => await Build(await _configuration.Get(id, cancellationToken).ConfigureAwait(false), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IDataSet>>> Get(CancellationToken cancellationToken = default)
    {
        var configurations = await _configuration.Get(cancellationToken).ConfigureAwait(false);
        if (configurations.IsFailure)
            return configurations.ToNewResult<IReadOnlyList<IDataSet>>();

        var dataSets = new List<IDataSet>();
        foreach (var configuration in configurations.Value ?? [])
        {
            var built = await Build(GenericResult<DataSetConfiguration>.Success(configuration), cancellationToken)
                .ConfigureAwait(false);
            if (built.IsFailure)
                return built.ToNewResult<IReadOnlyList<IDataSet>>();

            dataSets.Add(built.Value!);
        }

        return GenericResult<IReadOnlyList<IDataSet>>.Success(dataSets);
    }

    private async Task<IGenericResult<IDataSet>> Build(
        IGenericResult<DataSetConfiguration> configuration, CancellationToken cancellationToken)
    {
        if (configuration.IsFailure)
            return configuration.ToNewResult<IDataSet>();

        var builder = _builder();
        var configured = builder.Configure(configuration.Value!);
        return configured.IsFailure
            ? configured.ToNewResult<IDataSet>()
            : await builder.Build(cancellationToken).ConfigureAwait(false);
    }
}
