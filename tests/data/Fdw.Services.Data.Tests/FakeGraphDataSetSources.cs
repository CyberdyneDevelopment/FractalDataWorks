using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>A store of data sets and data stores for the check before save (FDW-5290), which records every name it is asked.</summary>
internal sealed class FakeGraphDataSetSources : IGraphDataSetSources
{
    /// <summary>Gets the data sets that exist, by name.</summary>
    public Dictionary<string, GraphDataSetSourceFacts> DataSetFacts { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the fields each data set stores, by data set name.</summary>
    public Dictionary<string, IReadOnlyList<GraphDataSetFieldFact>> StoredFields { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the fields each container stores, by data store then container name.</summary>
    public Dictionary<(string DataStore, string Container), IReadOnlyList<GraphDataSetFieldFact>> StoredContainerFields { get; } = [];

    /// <summary>Gets the data stores that exist.</summary>
    public HashSet<string> Stores { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets every data set name asked for, in the order asked, one entry per ask.</summary>
    public List<string> DataSetsAsked { get; } = [];

    /// <summary>Gets the containers each data store holds: data store, then container, then the paths it is in (more than one when the store holds the name in several).</summary>
    public Dictionary<string, Dictionary<string, List<string>>> StoreContainers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets every data store asked for its containers and the names asked, one entry per ask.</summary>
    public List<(string DataStore, string[] Names)> ContainersAsked { get; } = [];

    /// <summary>Gets or sets whether every read fails.</summary>
    public bool Fails { get; set; }

    /// <summary>Gets or sets whether only the read of a data store's containers fails.</summary>
    public bool ContainersFail { get; set; }

    /// <summary>Adds a data set that reads the given data sets.</summary>
    /// <param name="name">The data set.</param>
    /// <param name="reads">The data sets it reads.</param>
    /// <returns>This store.</returns>
    public FakeGraphDataSetSources WithDataSet(string name, params string[] reads)
    {
        DataSetFacts[name] = new GraphDataSetSourceFacts(true, false, reads, Guid.CreateVersion7());
        return this;
    }

    /// <summary>Adds an abstract Graph data set.</summary>
    /// <param name="name">The data set.</param>
    /// <returns>This store.</returns>
    public FakeGraphDataSetSources WithAbstractDataSet(string name)
    {
        DataSetFacts[name] = new GraphDataSetSourceFacts(true, true, [], Guid.CreateVersion7());
        return this;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>> DataSets(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        if (Fails)
        {
            return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>>(
                GenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>.Failure(
                    DataServiceResultCodes.ByName("GraphDataSetNotReadable"), NullLogger.Instance, ResultDetails.Create("dataSet", "any")));
        }

        DataSetsAsked.AddRange(names);
        var answer = new Dictionary<string, GraphDataSetSourceFacts>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            answer[name] = DataSetFacts.TryGetValue(name, out var facts) ? facts : new GraphDataSetSourceFacts(false, false, []);
        }

        return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>>(
            GenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>.Success(answer));
    }

    /// <summary>Adds a container a data store holds, in a path.</summary>
    /// <param name="dataStore">The data store.</param>
    /// <param name="container">The container.</param>
    /// <param name="path">The path it is in.</param>
    /// <returns>This store.</returns>
    public FakeGraphDataSetSources WithContainer(string dataStore, string container, string path)
    {
        Stores.Add(dataStore);
        if (!StoreContainers.TryGetValue(dataStore, out var held))
        {
            held = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            StoreContainers[dataStore] = held;
        }

        if (!held.TryGetValue(container, out var paths))
        {
            paths = [];
            held[container] = paths;
        }

        paths.Add(path);
        return this;
    }

    /// <summary>Gives a data set the fields it stores.</summary>
    /// <param name="name">The data set.</param>
    /// <param name="fields">The names of its fields, in order.</param>
    /// <returns>This store.</returns>
    public FakeGraphDataSetSources WithFields(string name, params string[] fields)
    {
        DataSetFacts[name] = new GraphDataSetSourceFacts(true, false, [], Guid.CreateVersion7());
        StoredFields[name] = [.. fields.Select(field => new GraphDataSetFieldFact(field, "String"))];
        return this;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>> Fields(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        if (Fails)
        {
            return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>>(
                GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>.Failure(
                    DataServiceResultCodes.ByName("GraphDataSetNotReadable"), NullLogger.Instance, ResultDetails.Create("dataSet", "any")));
        }

        return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>>(
            GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>.Success(
                names.Where(StoredFields.ContainsKey).ToDictionary(name => name, name => StoredFields[name], StringComparer.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>> Containers(
        string dataStore, IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        if (Fails || ContainersFail)
        {
            return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>>(
                GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Failure(
                    DataServiceResultCodes.ByName("GraphDataSetNotReadable"), NullLogger.Instance, ResultDetails.Create("dataSet", "any")));
        }

        ContainersAsked.Add((dataStore, [.. names]));
        var held = StoreContainers.TryGetValue(dataStore, out var stored) ? stored : new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, IReadOnlyList<string>> answer = names.Where(held.ContainsKey)
            .ToDictionary(name => name, name => (IReadOnlyList<string>)held[name], StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>>(GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Success(answer));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>> ContainerFields(
        string component, string dataStore, string? path, string container, ILogger logger, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>>(
            StoredContainerFields.FirstOrDefault(stored => string.Equals(stored.Key.DataStore, dataStore, StringComparison.OrdinalIgnoreCase) && string.Equals(stored.Key.Container, container, StringComparison.OrdinalIgnoreCase)) is { Value: { } fields }
                ? GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Success(fields)
                : GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(
                DataServiceResultCodes.ByName("ContainerFieldsNotFound"), logger, ResultDetails.Create("component", component, "dataStore", dataStore, "container", container)));

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlySet<string>>> DataStores(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlySet<string>>>(
            GenericResult<IReadOnlySet<string>>.Success(new HashSet<string>(names.Where(Stores.Contains), StringComparer.OrdinalIgnoreCase)));
}
