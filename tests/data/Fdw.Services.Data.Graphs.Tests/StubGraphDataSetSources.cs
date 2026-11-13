using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>Answers what each stored data set reads from the same definitions the nested gateway serves, so the two cannot disagree (FDW-5299).</summary>
internal sealed class StubGraphDataSetSources : IGraphDataSetSources
{
    private readonly IReadOnlyDictionary<string, GraphDataSetConfiguration> _graphs;
    private readonly IReadOnlyCollection<string> _tables;

    /// <summary>Initializes a new instance of the <see cref="StubGraphDataSetSources"/> class.</summary>
    /// <param name="graphs">The Graph data sets that exist, by name, without regard to case.</param>
    /// <param name="tables">The data sets that read nothing and exist.</param>
    public StubGraphDataSetSources(IReadOnlyDictionary<string, GraphDataSetConfiguration> graphs, IReadOnlyCollection<string> tables)
    {
        _graphs = graphs;
        _tables = tables;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>> DataSets(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>>(
            GenericResult<IReadOnlyDictionary<string, GraphDataSetSourceFacts>>.Success(
                names.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(name => name, Facts, StringComparer.OrdinalIgnoreCase)));

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>> Fields(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>>(
            GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphDataSetFieldFact>>>.Success(
                new Dictionary<string, IReadOnlyList<GraphDataSetFieldFact>>(StringComparer.OrdinalIgnoreCase)));

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>> Containers(
        string dataStore, IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>>(
            GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Success(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)));

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>> ContainerFields(
        string component, string dataStore, string? path, string container, ILogger logger, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyList<GraphDataSetFieldFact>>>(
            GenericResult<IReadOnlyList<GraphDataSetFieldFact>>.Failure(
                DataServiceResultCodes.ByName("ContainerFieldsNotFound"), logger, ResultDetails.Create("component", component, "dataStore", dataStore, "container", container)));

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlySet<string>>> DataStores(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlySet<string>>>(
            GenericResult<IReadOnlySet<string>>.Success(new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

    private GraphDataSetSourceFacts Facts(string name) =>
        _graphs.TryGetValue(name, out var graph)
            ? new GraphDataSetSourceFacts(
                true,
                false,
                [.. GraphDataSetReads.Of(graph).Where(read => !read.IsIntended && string.Equals(read.Kind, GraphDataSetReadKinds.DataSet, StringComparison.Ordinal)).Select(read => read.Name)])
            : new GraphDataSetSourceFacts(_tables.Contains(name, StringComparer.OrdinalIgnoreCase), false, []);
}
