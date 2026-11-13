using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>Data sets and data stores for the check before save (FDW-5290): the names given exist, every other does not.</summary>
internal sealed class StubGraphDataSetSources : IGraphDataSetSources
{
    private readonly Dictionary<string, GraphDataSetSourceFacts> _dataSets = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _stores = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<GraphDataSetFieldFact>> _fields = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> _containers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<GraphDataSetFieldFact>> _containerFields = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds a data set that exists.</summary>
    /// <param name="name">The data set.</param>
    /// <param name="isUnboundGraph">Whether it is an abstract Graph data set.</param>
    /// <returns>This stub.</returns>
    public StubGraphDataSetSources WithDataSet(string name, bool isUnboundGraph = false)
    {
        _dataSets[name] = new GraphDataSetSourceFacts(true, isUnboundGraph, [], Guid.CreateVersion7());
        return this;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>> DataSets(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        var answer = names.ToDictionary(
            name => name, name => _dataSets.TryGetValue(name, out var facts) ? facts : new GraphDataSetSourceFacts(false, false, []), StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>>(
            GenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>.Success(answer));
    }

    /// <summary>Gives a data set that exists the fields it stores.</summary>
    /// <param name="name">The data set.</param>
    /// <param name="fields">The names of its fields, in order.</param>
    /// <returns>This stub.</returns>
    public StubGraphDataSetSources WithFields(string name, params string[] fields)
    {
        WithDataSet(name);
        _fields[name] = [.. fields.Select(field => new GraphDataSetFieldFact(field, "String"))];
        return this;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>> Fields(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>>(
            GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>.Success(
                names.Where(_fields.ContainsKey).ToDictionary(name => name, name => _fields[name], StringComparer.OrdinalIgnoreCase)));

    /// <summary>Adds a data store that holds a container in a path.</summary>
    /// <param name="dataStore">The data store.</param>
    /// <param name="container">The container.</param>
    /// <param name="path">The path it is in.</param>
    /// <returns>This stub.</returns>
    public StubGraphDataSetSources WithContainer(string dataStore, string container, string path)
    {
        _stores.Add(dataStore);
        _containers[container] = [path];
        return this;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>> Containers(
        string dataStore, IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>>(
            GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Success(
                names.Where(_containers.ContainsKey).ToDictionary(name => name, name => _containers[name], StringComparer.OrdinalIgnoreCase)));

    /// <summary>Gives a container of a data store the fields it stores.</summary>
    /// <param name="dataStore">The data store.</param>
    /// <param name="container">The container.</param>
    /// <param name="path">The path it is in.</param>
    /// <param name="fields">Its fields, in order.</param>
    /// <returns>This stub.</returns>
    public StubGraphDataSetSources WithContainerFields(string dataStore, string container, string path, params GraphDataSetFieldFact[] fields)
    {
        WithContainer(dataStore, container, path);
        _containerFields[$"{dataStore}/{container}"] = fields;
        return this;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>> ContainerFields(
        string component, string dataStore, string? path, string container, ILogger logger, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>>(
            _containerFields.TryGetValue($"{dataStore}/{container}", out var fields)
                ? GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Success(fields)
                : GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(
                DataServiceResultCodes.ByName("ContainerFieldsNotFound"), logger, ResultDetails.Create("component", component, "dataStore", dataStore, "container", container)));

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlySet<string>>> DataStores(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlySet<string>>>(
            GenericResult<IReadOnlySet<string>>.Success(new HashSet<string>(names.Where(_stores.Contains), StringComparer.OrdinalIgnoreCase)));
}
