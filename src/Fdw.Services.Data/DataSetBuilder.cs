using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DataFieldConfiguration = Fdw.Data.DataSets.Abstractions.DataFieldConfiguration;
using DataSetCompositionTypes = Fdw.Data.Abstractions.DataSetCompositionTypes;
using DataSetConfiguration = Fdw.Data.DataSets.Abstractions.DataSetConfiguration;
using DataSetSourceConfiguration = Fdw.Data.DataSets.Abstractions.DataSetSourceConfiguration;
using IDataSetBuilder = Fdw.Data.DataSets.Abstractions.IDataSetBuilder;
using JoinConfiguration = Fdw.Data.DataSets.Abstractions.JoinConfiguration;

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of <see cref="IDataSetBuilder"/>.
/// Builds live <see cref="IDataSet"/> runtime instances from <see cref="DataSetConfiguration"/> records.
/// </summary>
public sealed class DataSetBuilder : IDataSetBuilder
{
    private readonly ILogger<DataSetBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DataSetBuilder"/>.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public DataSetBuilder(ILogger<DataSetBuilder>? logger = null)
    {
        _logger = logger ?? NullLogger<DataSetBuilder>.Instance;
    }

    /// <inheritdoc />
    private DataSetConfiguration? _config;

    /// <inheritdoc />
    public IGenericResult Configure(DataSetConfiguration dataSetConfig)
    {
        ArgumentNullException.ThrowIfNull(dataSetConfig);
        _config = dataSetConfig;
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public Task<IGenericResult<IDataSet>> Build(CancellationToken cancellationToken = default)
        => Task.FromResult(BuildInternal());

    private IGenericResult<IDataSet> BuildInternal()
    {
        if (_config is null)
            return GenericResult<IDataSet>.Failure(DataSetBuilderLog.NotConfigured(_logger));

        var config = _config;
        DataSetBuilderLog.TraceCreate(_logger, config.Name);

        if (string.IsNullOrWhiteSpace(config.Name))
        {
            return GenericResult<IDataSet>.Failure(
                DataSetBuilderLog.ConfigurationNameRequired(_logger));
        }

        // Resolve the composition strategy from the TypeCollection.
        var composition = ResolveComposition(config);

        IReadOnlyList<DataSetSourceConfiguration> sourceConfigs =
            config.Sources is IReadOnlyList<DataSetSourceConfiguration> ro ? ro
            : config.Sources?.ToList() ?? [];

        var sources = sourceConfigs
            .Select(s => (IDataSetSource)new DataSetRuntimeSource(s))
            .ToList()
            .AsReadOnly();

        // Build source lookup for join resolution.
        var sourcesByName = sources
            .Select((s, i) => (Source: s, Config: sourceConfigs[i]))
            .ToDictionary(
                x => x.Config.SourceName,
                x => x.Source,
                StringComparer.OrdinalIgnoreCase);

        // Build join runtimes.
        IReadOnlyList<IDataSetJoin> joins;
        if (config.Joins.Count > 0 && sourcesByName.Count > 0)
        {
            var joinList = new List<IDataSetJoin>(config.Joins.Count);
            foreach (var joinConfig in config.Joins)
            {
                try
                {
                    joinList.Add(new DataSetRuntimeJoin(joinConfig, sourcesByName));
                }
                catch (ArgumentException ex)
                {
                    return GenericResult<IDataSet>.Failure(
                        DataSetBuilderLog.JoinBuildFailed(_logger, config.Name, ex.Message));
                }
            }
            joins = joinList.AsReadOnly();
        }
        else
        {
            joins = Array.Empty<IDataSetJoin>();
        }

        // Build field runtimes.
        var fields = config.Fields
            .Select(f => (IDataField)new DataSetRuntimeField(f))
            .ToList()
            .AsReadOnly();

        IReadOnlyList<IContainerKey> keys = Array.Empty<IContainerKey>();

        var dataSet = new DataSet(
            config.Name,
            config.Description,
            composition,
            sources,
            joins,
            fields,
            keys);

        DataSetBuilderLog.Created(_logger, config.Name, sources.Count, fields.Count);
        return GenericResult<IDataSet>.Success(dataSet);
    }

    /// <summary>
    /// Determines the composition strategy from the configuration.
    /// </summary>
    private static IDataSetCompositionType ResolveComposition(DataSetConfiguration config)
    {
        var sourceCount = config.Sources?.Count ?? 0;
        if (sourceCount <= 1 || config.Joins.Count == 0)
        {
            if (sourceCount > 1)
                return DataSetCompositionTypes.ByName("Union");
            return DataSetCompositionTypes.ByName("Singular");
        }
        return DataSetCompositionTypes.ByName("Join");
    }
}
