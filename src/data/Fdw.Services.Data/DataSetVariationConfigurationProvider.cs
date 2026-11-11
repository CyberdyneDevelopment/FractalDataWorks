using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Supplies the data sets that are variations: the data set provider's members of kind Variation (FDW-3638).</summary>
/// <remarks>
/// A variation is a data set stored in <c>data.DataSet</c> and returned by <see cref="IDataSetConfigurationProvider"/> with the rest, so a
/// member read, a save and a retire go through that provider and nothing here owns a row. What this adds is the narrowing to variations and
/// the list reads: by owner, by the data set it varies, by its parent and by its name, which <see cref="DataSetVariationListProvider"/>
/// answers from one view.
/// </remarks>
public sealed class DataSetVariationConfigurationProvider : IDataSetVariationConfigurationProvider
{
    private const string DomainName = "DataSet";

    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly DataSetVariationListProvider _list;
    private readonly ILogger<DataSetVariationConfigurationProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetVariationConfigurationProvider"/> class.</summary>
    /// <param name="dataSets">The data set provider, which holds every kind of data set.</param>
    /// <param name="list">Reads the list of variations.</param>
    /// <param name="logger">Logger for this provider instance.</param>
    public DataSetVariationConfigurationProvider(
        IDataSetConfigurationProvider dataSets,
        DataSetVariationListProvider list,
        ILogger<DataSetVariationConfigurationProvider>? logger)
    {
        _dataSets = dataSets;
        _list = list;
        _logger = logger ?? NullLogger<DataSetVariationConfigurationProvider>.Instance;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(string name, CancellationToken ct = default)
        => Get(name, ConfigurationLevels.WholeAggregate, ct);

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(string name, int levels, CancellationToken ct = default)
        => Narrow(await _dataSets.Get(name, levels, ct).ConfigureAwait(false), name);

    /// <inheritdoc/>
    public Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(Guid id, CancellationToken ct = default)
        => Get(id, ConfigurationLevels.WholeAggregate, ct);

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(Guid id, int levels, CancellationToken ct = default)
        => Narrow(await _dataSets.Get(id, levels, ct).ConfigureAwait(false), id.ToString("D"));

    /// <inheritdoc/>
    public Task<IGenericResult<ConfigurationPage<DataSetVariationDomainConfiguration>>> Get(DomainQuery query, CancellationToken ct = default)
        => _list.Get(query, ct);

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>> Get(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => _list.Get(ids, ct);

    /// <inheritdoc/>
    public Task<IGenericResult> Save(IDataSetVariationImplementationConfiguration variation, string name, CancellationToken ct = default)
    {
        DataSetVariationProviderLog.Saving(_logger, name);
        return _dataSets.Save(variation, DomainName, VariationDataSetConfiguration.ImplementationName, name, ct);
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Update(IDataSetVariationImplementationConfiguration variation, string name, CancellationToken ct = default)
    {
        DataSetVariationProviderLog.Saving(_logger, name);
        return _dataSets.Update(variation, DomainName, VariationDataSetConfiguration.ImplementationName, name, ct);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
    {
        var found = await Get(id, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (found.IsFailure)
            return found;

        if (found.Value is null)
            return GenericResult.Failure(DataSetVariationProviderLog.DeleteRefused(_logger, id));

        DataSetVariationProviderLog.Retiring(_logger, id);
        return await _dataSets.Delete(id, ct).ConfigureAwait(false);
    }

    // A data set of another kind is not a variation, which is an answer and not a failure: the same as no data set with that name.
    private IGenericResult<IDataSetVariationImplementationConfiguration?> Narrow(IGenericResult<IDataSetImplementationConfiguration> read, string key)
    {
        if (read.IsFailure)
            return read.ToNewResult<IDataSetVariationImplementationConfiguration?>();

        if (read.Value is null)
            return GenericResult<IDataSetVariationImplementationConfiguration?>.Success(null);

        if (read.Value is IDataSetVariationImplementationConfiguration variation)
            return GenericResult<IDataSetVariationImplementationConfiguration?>.Success(variation);

        DataSetVariationProviderLog.NotAVariation(_logger, key, read.Value.Implementation);
        return GenericResult<IDataSetVariationImplementationConfiguration?>.Success(null);
    }
}
