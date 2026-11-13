using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Results.Abstractions;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Fdw.Services.Connections;

namespace Fdw.Services.Data;

/// <summary>Reads the sources of a Graph data set draft through the data set and data store configuration providers (FDW-5290).</summary>
/// <remarks>
/// Each distinct name is one read of the row and its direct children, which is all the check needs: whether it exists, whether it is an abstract
/// Graph data set, and the names it reads. A name that does not exist is an empty read of the provider, not a failure.
/// </remarks>
public sealed class GraphDataSetSources : IGraphDataSetSources
{
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataStoreConfigurationProvider _dataStores;

    /// <summary>Initializes a new instance of the <see cref="GraphDataSetSources"/> class.</summary>
    /// <param name="dataSets">The data set domain's configuration provider.</param>
    /// <param name="dataStores">The data store domain's configuration provider.</param>
    public GraphDataSetSources(IDataSetConfigurationProvider dataSets, IDataStoreConfigurationProvider dataStores)
    {
        _dataSets = dataSets;
        _dataStores = dataStores;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>> DataSets(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        var facts = new Dictionary<string, GraphDataSetSourceFacts>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var read = await GraphDataSetDefinitions.Read(_dataSets, name, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
            {
                return read.ToNewResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>();
            }

            facts[name] = read.Value is { } found
                ? new GraphDataSetSourceFacts(true, found is IGraphDataSetConfiguration graph && !graph.IsBound(), GraphDataSetNestedReads.Of(found), found.Id)
                : new GraphDataSetSourceFacts(false, false, []);
        }

        return GenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>.Success(facts);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlySet<string>>> DataStores(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var read = await _dataStores.Get(name, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
            {
                return read.ToNewResult<IReadOnlySet<string>>();
            }

            if (read.Value is not null)
            {
                existing.Add(name);
            }
        }

        return GenericResult<IReadOnlySet<string>>.Success(existing);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>> Fields(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        var answer = new Dictionary<string, IReadOnlyList<GraphDataSetFieldFact>>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var read = await _dataSets.Get(name, ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
            {
                return read.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>();
            }

            if (read.Value is { } found)
            {
                answer[name] = [.. found.Fields.Where(field => !field.IsDeleted).OrderBy(field => field.Ordinal).Select(field => new GraphDataSetFieldFact(field.Name, field.TypeName))];
            }
        }

        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>.Success(answer);
    }

    /// <inheritdoc />
    /// <remarks>
    /// One read of the data store narrowed on the container names, as the data store provider reads the containers it builds (FDW-4328): the store's own
    /// stored rows, never its connection. A store that is not there answers no container; any other failure of the read is the answer.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>> Containers(
        string dataStore, IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        var read = await _dataStores.Get(
            dataStore,
            ConfigurationLevels.WholeAggregate,
            new ChildNarrowing(typeof(DataContainerConfiguration), nameof(DataContainerConfiguration.Name), [.. names.Distinct(StringComparer.OrdinalIgnoreCase)]),
            cancellationToken).ConfigureAwait(false);
        var found = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (read.IsFailure)
        {
            // Why only an absent store is no failure: the check judges a data store that is not there apart, so it holds no container here; any other failure of the read is the answer.
            return FailureCategory.IsMissing(read)
                ? Answer(found)
                : read.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>();
        }

        // Why every path is kept and none is picked: the data layer makes a container name unique within a PATH (UX_DataContainer_DataPathId_Name_Current), not within a store, so
        // one name can be held in two paths and the check says so (GraphDataSetContainerAmbiguous) instead of choosing one.
        foreach (var path in read.Value?.Paths ?? [])
        {
            foreach (var container in path.Containers)
            {
                if (!found.TryGetValue(container.Name, out var paths))
                {
                    paths = [];
                    found[container.Name] = paths;
                }

                paths.Add(path.Name);
            }
        }

        return Answer(found);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The same narrowed read of the data store as <see cref="Containers"/>, kept whole with the container's own fields (FDW-5523). A path that is stated picks that path; no path and one
    /// path holding the name is that one; no path and the name in several paths is a failure, never a pick. The type of a field is its portable type, <c>FrameworkDataType</c>, the vocabulary
    /// a data set field's type uses; where nobody has stated it the field has no type, which is what it is, not a substitute.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>> ContainerFields(
        string component, string dataStore, string? path, string container, ILogger logger, CancellationToken cancellationToken = default)
    {
        var details = ResultDetails.Create("component", component, "dataStore", dataStore, "container", container);
        var read = await _dataStores.Get(
            dataStore,
            ConfigurationLevels.WholeAggregate,
            new ChildNarrowing(typeof(DataContainerConfiguration), nameof(DataContainerConfiguration.Name), [container]),
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return FailureCategory.IsMissing(read)
                ? GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(DataServiceResultCodes.ByName("ContainerFieldsStoreNotFound"), logger, details)
                : read.ToNewResult<IReadOnlyList<GraphDataSetFieldFact>>();
        }

        // Why a store that comes back empty is a store not found: a store that is not there is answered as no store, as it is for Containers.
        if (read.Value is null)
        {
            return GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(DataServiceResultCodes.ByName("ContainerFieldsStoreNotFound"), logger, details);
        }

        var held = read.Value.Paths
            .SelectMany(storePath => storePath.Containers.Where(candidate => string.Equals(candidate.Name, container, StringComparison.OrdinalIgnoreCase)).Select(candidate => (Path: storePath.Name, Container: candidate)))
            .ToList();
        if (held.Count == 0)
        {
            return GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(DataServiceResultCodes.ByName("ContainerFieldsNotFound"), logger, details);
        }

        // Why no pick: the data layer makes a container name unique within a PATH, not within a store, so a name held in several paths with none stated is the caller's to settle.
        var chosen = held;
        if (!string.IsNullOrEmpty(path))
        {
            chosen = [.. held.Where(candidate => string.Equals(candidate.Path, path, StringComparison.OrdinalIgnoreCase))];
            if (chosen.Count == 0)
            {
                return GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(
                    DataServiceResultCodes.ByName("ContainerFieldsInOtherPath"), logger, details.With("path", path).With("foundPath", string.Join(", ", held.Select(candidate => candidate.Path))));
            }
        }
        else if (held.Count > 1)
        {
            return GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(
                DataServiceResultCodes.ByName("ContainerFieldsAmbiguous"), logger, details.With("paths", string.Join(", ", held.Select(candidate => candidate.Path))));
        }

        return GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Success(
            [.. chosen[0].Container.Fields.Where(field => !field.IsDeleted).OrderBy(field => field.Ordinal).Select(field => new GraphDataSetFieldFact(field.Name, field.FrameworkDataType))]);
    }

    private static IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>> Answer(Dictionary<string, List<string>> found) =>
        GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Success(
            found.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<string>)[.. entry.Value.Distinct(StringComparer.OrdinalIgnoreCase)], StringComparer.OrdinalIgnoreCase));
}
