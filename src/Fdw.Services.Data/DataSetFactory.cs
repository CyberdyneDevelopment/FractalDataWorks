using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// Why: Use fully qualified aliases to avoid IDataField ambiguity — both Fdw.Data.Abstractions
// and Fdw.Data.DataSets.Abstractions define IDataField with different contracts.
// The IDataField we work with here is from Data.Abstractions (IDataNode hierarchy).
using DataFieldConfiguration = Fdw.Data.DataSets.Abstractions.DataFieldConfiguration;
using DataSetCompositionTypes = Fdw.Data.Abstractions.DataSetCompositionTypes;
using DataSetConfiguration = Fdw.Data.DataSets.Abstractions.DataSetConfiguration;
using DataSetSourceConfiguration = Fdw.Data.DataSets.Abstractions.DataSetSourceConfiguration;
using IDataSetFactory = Fdw.Data.DataSets.Abstractions.IDataSetFactory;
using JoinConfiguration = Fdw.Data.DataSets.Abstractions.JoinConfiguration;

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of <see cref="IDataSetFactory"/>.
/// Builds live <see cref="IDataSet"/> runtime instances from <see cref="DataSetConfiguration"/> records.
/// </summary>
public sealed class DataSetFactory : IDataSetFactory
{
    private readonly ILogger<DataSetFactory> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DataSetFactory"/>.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public DataSetFactory(ILogger<DataSetFactory>? logger = null)
    {
        _logger = logger ?? NullLogger<DataSetFactory>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<IDataSet> Create(DataSetConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        DataSetFactoryLog.TraceCreate(_logger, config.Name);

        if (string.IsNullOrWhiteSpace(config.Name))
        {
            return GenericResult<IDataSet>.Failure(
                DataSetFactoryLog.ConfigurationNameRequired(_logger));
        }

        // Resolve the composition strategy from the TypeCollection.
        // Why: DataSetConfiguration does not carry a composition field directly; composition is
        // derived from the number of sources and join configuration. Singular = 1 source, no joins.
        // Join = multiple sources with explicit joins. Union = multiple sources, no joins.
        var composition = ResolveComposition(config);

        // Why: Sources are composed by DataSetConfigurationProvider.Get — no resolver needed.
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
                        DataSetFactoryLog.JoinBuildFailed(_logger, config.Name, ex.Message));
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

        // Why: Keys are not populated from DataSetConfiguration — keys come from the DataStore
        // tree (IContainerKey on IDataContainer). DataSet-level key fields (DataSetKeyFieldConfiguration)
        // are query-layer hints, not the same as container-layer structural keys.
        IReadOnlyList<IContainerKey> keys = Array.Empty<IContainerKey>();

        var dataSet = new DataSet(
            config.Name,
            config.Description,
            composition,
            sources,
            joins,
            fields,
            keys);

        DataSetFactoryLog.Created(_logger, config.Name, sources.Count, fields.Count);
        return GenericResult<IDataSet>.Success(dataSet);
    }

    /// <summary>
    /// Determines the composition strategy from the configuration.
    /// </summary>
    private static IDataSetCompositionType ResolveComposition(DataSetConfiguration config)
    {
        // Why: Composition is inferred from Sources count and Joins presence.
        // Singular = 0 or 1 source, no explicit joins.
        // Join = 2+ sources with joins defined.
        // Union = 2+ sources, no joins defined (federated union).
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
