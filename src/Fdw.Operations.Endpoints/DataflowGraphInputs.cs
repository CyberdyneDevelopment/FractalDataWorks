using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Everything the dataflow graph layers draw from, read once: the pipelines, and the data sets,
/// datastores and sources they reach.
/// </summary>
internal sealed class DataflowGraphInputs
{
    private readonly IReadOnlyList<DataSetRecord> _dataSets;
    private readonly IReadOnlyList<IDataSetImplementationConfiguration> _owners;
    private readonly IReadOnlyList<DataSetVariationDomainConfiguration> _allVariations;
    private readonly Dictionary<Guid, DataSetRecord> _recordsById;

    /// <summary>Initializes the inputs and works out which data sets the pipelines reach.</summary>
    public DataflowGraphInputs(
        IReadOnlyList<DataSetRecord> dataSets,
        IReadOnlyList<DataStoreRecord> dataStores,
        IReadOnlyList<IDataSetImplementationConfiguration> owners,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetVariationDomainConfiguration> variations)
    {
        _dataSets = dataSets;
        _owners = owners;
        _allVariations = variations;
        DataStores = dataStores;
        Pipelines = pipelines;
        _recordsById = dataSets.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
        ReachableOwners = DataflowReachability.Owners(pipelines, owners).Where(o => _recordsById.ContainsKey(o.Id)).ToList();
        var reachableIds = ReachableOwners.Select(o => o.Id).ToHashSet();
        Variations = variations.Where(v => reachableIds.Contains(v.BaseDataSetId)).ToList();
    }

    /// <summary>Every datastore.</summary>
    public IReadOnlyList<DataStoreRecord> DataStores { get; }

    /// <summary>The pipelines the graph is rooted at.</summary>
    public IReadOnlyList<PipelineLineageRecord> Pipelines { get; }

    /// <summary>The data sets the pipelines reach, each with a record to draw it from.</summary>
    public IReadOnlyList<IDataSetImplementationConfiguration> ReachableOwners { get; }

    /// <summary>
    /// The variations of the data sets the pipelines reach. A variation reaches nothing itself: it is
    /// drawn because its base is, and never makes another data set reachable.
    /// </summary>
    public IReadOnlyList<DataSetVariationDomainConfiguration> Variations { get; }

    /// <summary>The record a reachable data set is drawn from.</summary>
    public DataSetRecord RecordFor(IDataSetImplementationConfiguration owner) => _recordsById[owner.Id];

    /// <summary>The reachable data set of the name, or null when no pipeline reaches one.</summary>
    public IDataSetImplementationConfiguration? ReachableNamed(string name)
        => ReachableOwners.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The datastore of the name, or null when none is configured under it.</summary>
    public DataStoreRecord? StoreNamed(string name)
        => DataStores.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether a pipeline of the name exists.</summary>
    public bool HasPipeline(string name)
        => Pipelines.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The same inputs rooted at the one pipeline of the name.</summary>
    public DataflowGraphInputs ForPipeline(string name)
        => new(_dataSets, DataStores, _owners,
            Pipelines.Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList(),
            _allVariations);
}
