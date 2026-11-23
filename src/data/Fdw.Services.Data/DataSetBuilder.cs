using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DataSetFieldConfiguration = Fdw.Data.DataSets.Abstractions.DataSetFieldConfiguration;
using DataSetCompositionTypes = Fdw.Data.Abstractions.DataSetCompositionTypes;
using DataSetSourceConfiguration = Fdw.Data.DataSets.Abstractions.DataSetSourceConfiguration;
using IDataSetImplementationConfiguration = Fdw.Data.DataSets.Abstractions.IDataSetImplementationConfiguration;
using IDataSetBuilder = Fdw.Data.DataSets.Abstractions.IDataSetBuilder;
using DataSetImplementationConfigurationExtensions = Fdw.Data.DataSets.Abstractions.DataSetImplementationConfigurationExtensions;
using IJoinedDataSetConfiguration = Fdw.Data.DataSets.Abstractions.IJoinedDataSetConfiguration;
using SourceCombinationRules = Fdw.Data.DataSets.Abstractions.SourceCombinationRules;

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of <see cref="IDataSetBuilder"/>.
/// Builds live <see cref="IDataSet"/> runtime instances from <see cref="IDataSetImplementationConfiguration"/> records.
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
    private IDataSetImplementationConfiguration? _config;

    /// <inheritdoc />
    public IGenericResult Configure(IDataSetImplementationConfiguration dataSetConfig)
    {
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

        // The composition the data set states (FDW-6946); refused when it states none for several sources or one its joins contradict.
        var composition = ResolveComposition(config);
        if (composition.IsFailure)
            return composition.ToNewResult<IDataSet>();

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
        var configuredJoins = DataSetImplementationConfigurationExtensions.JoinsOrNone(config);
        if (configuredJoins.Count > 0 && sourcesByName.Count > 0)
        {
            var joinList = new List<IDataSetJoin>(configuredJoins.Count);
            foreach (var joinConfig in configuredJoins.OrderBy(j => j.Ordinal))
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
            composition.Value!,
            sources,
            joins,
            fields,
            keys);

        DataSetBuilderLog.Created(_logger, config.Name, sources.Count, fields.Count);
        return GenericResult<IDataSet>.Success(dataSet);
    }

    /// <summary>
    /// The composition a data set states (FDW-6946): a Compound or Federated data set with several sources states Join or Union, checked by
    /// <see cref="SourceCombinationRules"/>; a data set of one source, or of a kind that reads one, is Singular. Nothing is worked out from
    /// whether the data set has joins.
    /// </summary>
    private IGenericResult<IDataSetCompositionType> ResolveComposition(IDataSetImplementationConfiguration config)
    {
        if (config is not IJoinedDataSetConfiguration joined)
            return GenericResult<IDataSetCompositionType>.Success(DataSetCompositionTypes.ByName("Singular"));

        var checkedCombination = SourceCombinationRules.Validate(joined, _logger);
        if (checkedCombination.IsFailure)
            return checkedCombination.ToNewResult<IDataSetCompositionType>();

        return GenericResult<IDataSetCompositionType>.Success(string.IsNullOrWhiteSpace(joined.SourceCombination)
            ? DataSetCompositionTypes.ByName("Singular")
            : SourceCombinationRules.Stated(joined));
    }
}
