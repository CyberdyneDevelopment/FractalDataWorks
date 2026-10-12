using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of <see cref="IDataSetProvider"/>.
/// Resolves DataSet configurations via <see cref="IDataSetConfigurationProvider"/> and
/// builds live <see cref="IDataSet"/> runtimes via <see cref="IDataSetBuilder"/>.
/// </summary>
public sealed class DataSetRuntimeProvider : IDataSetProvider
{
    private readonly IDataSetConfigurationProvider _configurationProvider;
    private readonly IDataSetBuilder _factory;
    private readonly ILogger<DataSetRuntimeProvider> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DataSetRuntimeProvider"/>.
    /// </summary>
    /// <param name="configurationProvider">Provides DataSet configuration records.</param>
    /// <param name="factory">Creates live DataSet runtimes from configuration records.</param>
    /// <param name="logger">Optional logger instance.</param>
    public DataSetRuntimeProvider(
        IDataSetConfigurationProvider configurationProvider,
        IDataSetBuilder factory,
        ILogger<DataSetRuntimeProvider>? logger = null)
    {
        _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? NullLogger<DataSetRuntimeProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataSet>> Get(string name, CancellationToken cancellationToken = default)
    {
        DataSetRuntimeProviderLog.TraceGet(_logger, name);

        if (string.IsNullOrWhiteSpace(name))
        {
            return GenericResult<IDataSet>.Failure(
                DataServiceResultCodes.ByName("DataSetNameRequired"), _logger);
        }

        var configResult = await _configurationProvider.Get(name, cancellationToken).ConfigureAwait(false);
        if (!configResult.IsSuccess)
        {
            DataSetRuntimeProviderLog.ConfigurationNotFound(_logger, name);
            return configResult.Messages.Any()
                ? configResult.ToNewResult<IDataSet>()
                : GenericResult<IDataSet>.Failure(
                    DataServiceResultCodes.ByName("DataSetNotFound"),
                    ResultDetails.Create().With("DataSetName", name));
        }

        var configured = _factory.Configure(configResult.Value!);
        if (!configured.IsSuccess)
            return configured.ToNewResult<IDataSet>();

        var buildResult = await _factory.Build(cancellationToken).ConfigureAwait(false);
        if (!buildResult.IsSuccess)
        {
            return buildResult.Messages.Any()
                ? buildResult
                : GenericResult<IDataSet>.Failure(
                    DataSetRuntimeProviderLog.BuildFailed(_logger, name));
        }

        DataSetRuntimeProviderLog.Retrieved(_logger, name);
        return buildResult;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataSet>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        DataSetRuntimeProviderLog.TraceGetById(_logger, id);

        var configResult = await _configurationProvider.Get(id, cancellationToken).ConfigureAwait(false);
        if (!configResult.IsSuccess)
        {
            DataSetRuntimeProviderLog.ConfigurationNotFoundById(_logger, id);
            return configResult.Messages.Any()
                ? configResult.ToNewResult<IDataSet>()
                : GenericResult<IDataSet>.Failure(
                    DataServiceResultCodes.ByName("DataSetNotFound"),
                    ResultDetails.Create().With("DataSetId", id));
        }

        var configured = _factory.Configure(configResult.Value!);
        if (!configured.IsSuccess)
            return configured.ToNewResult<IDataSet>();

        var buildResult = await _factory.Build(cancellationToken).ConfigureAwait(false);
        if (!buildResult.IsSuccess)
        {
            return buildResult.Messages.Any()
                ? buildResult
                : GenericResult<IDataSet>.Failure(
                    DataSetRuntimeProviderLog.BuildFailed(_logger, configResult.Value?.Name ?? id.ToString()));
        }

        DataSetRuntimeProviderLog.RetrievedById(_logger, id);
        return buildResult;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IDataSet>>> Get(CancellationToken cancellationToken = default)
    {
        DataSetRuntimeProviderLog.TraceGetAll(_logger);

        var allConfigsResult = await _configurationProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!allConfigsResult.IsSuccess)
        {
            return allConfigsResult.Messages.Any()
                ? allConfigsResult.ToNewResult<IReadOnlyList<IDataSet>>()
                : GenericResult<IReadOnlyList<IDataSet>>.Failure(
                    DataSetRuntimeProviderLog.AllConfigsLoadFailed(_logger));
        }

        var dataSets = new List<IDataSet>(allConfigsResult.Value!.Count);
        var failureCount = 0;
        foreach (var config in allConfigsResult.Value!)
        {
            var configured = _factory.Configure(config);
            if (!configured.IsSuccess)
            {
                failureCount++;
                continue;
            }

            var buildResult = await _factory.Build(cancellationToken).ConfigureAwait(false);
            if (buildResult.IsSuccess)
            {
                dataSets.Add(buildResult.Value!);
            }
            else
            {
                failureCount++;
            }
        }

        if (failureCount > 0)
        {
            DataSetRuntimeProviderLog.SomeBuildsFailed(_logger, failureCount, allConfigsResult.Value!.Count);
        }

        DataSetRuntimeProviderLog.AllRetrieved(_logger, dataSets.Count);
        return GenericResult<IReadOnlyList<IDataSet>>.Success(dataSets.AsReadOnly());
    }
}
