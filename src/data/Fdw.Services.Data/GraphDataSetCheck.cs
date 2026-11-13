using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// The check before save of a Graph data set draft (FDW-5290): every problem saving would be refused for, each with its own code and the
/// path of the field out, component or link it concerns, and the intended sources that are not bound yet.
/// </summary>
/// <remarks>
/// <para>
/// It judges the stored body as the draft states it, and reads other data sets and data stores only to say whether they exist, whether an
/// abstract one is read, and what they read in turn. It lists every problem, where <see cref="GraphDataSetRefusals.Check"/>, which it reuses the
/// families of, answers the first. It judges no name, so one check serves a new data set and the edit of a saved one.
/// </para>
/// <para>
/// A draft with no graph (abstract) has nothing fed and reads nothing: its intended sources are reported as not bound yet and never block, and
/// an abstract draft that reads an abstract data set is accepted. A bound draft that reads an abstract data set is refused. What the engine's
/// own check of a graph's shape can say about the components is the engine slice of this story, not here.
/// </para>
/// </remarks>
public static class GraphDataSetCheck
{
    private const string FieldsOut = "fieldsOut";
    private const string Components = "components";
    private const string IntendedSources = "intendedSources";

    /// <summary>Checks a Graph data set draft.</summary>
    /// <param name="draft">The draft, read from the request exactly as save reads it.</param>
    /// <param name="sources">Reads the data sets and data stores the draft reads or intends to read.</param>
    /// <param name="logger">The logger each problem is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Every problem and every intended source not bound yet, or the failure of a read.</returns>
    public static async Task<IGenericResult<GraphDataSetCheckResult>> Check(
        IGraphDataSetConfiguration draft, IGraphDataSetSources sources, ILogger logger, CancellationToken cancellationToken = default)
    {
        var problems = new List<GraphDataSetProblem>();
        problems.AddRange(DuplicateFields(draft, logger));
        problems.AddRange(DuplicateComponents(draft, logger));
        problems.AddRange(RefusedComponents(draft, logger));
        if (draft.IsBound())
        {
            problems.AddRange(UnfedFields(draft, logger));
        }

        var facts = new Dictionary<string, GraphDataSetSourceFacts>(StringComparer.OrdinalIgnoreCase);
        var read = await ReadSources(draft, sources, facts, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read.ToNewResult<GraphDataSetCheckResult>();
        }

        problems.AddRange(MissingSources(draft, facts, read.Value, logger));
        var containers = await ReadContainers(draft, sources, read.Value, cancellationToken).ConfigureAwait(false);
        if (containers.IsFailure || containers.Value is null)
        {
            return containers.ToNewResult<GraphDataSetCheckResult>();
        }

        problems.AddRange(ContainerProblems(draft, containers.Value, read.Value, logger));
        FillIntentIds(draft, facts);
        problems.AddRange(IntentProblems(draft, facts, logger));
        if (draft.IsBound())
        {
            problems.AddRange(UnboundSources(draft, facts, logger));
        }

        var cycle = await Cycle(draft, sources, facts, logger, cancellationToken).ConfigureAwait(false);
        if (cycle.IsFailure || cycle.Value is null)
        {
            return cycle.ToNewResult<GraphDataSetCheckResult>();
        }

        problems.AddRange(cycle.Value);
        return GenericResult<GraphDataSetCheckResult>.Success(new GraphDataSetCheckResult(problems, NotBound(draft, facts, read.Value)));
    }

    private static IEnumerable<GraphDataSetProblem> DuplicateFields(IGraphDataSetConfiguration draft, ILogger logger)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return draft.GraphFields
            .Where(field => !seen.Add(field.Name) && reported.Add(field.Name))
            .Select(field => GraphDataSetProblems.Of("GraphDataSetFieldOutDuplicate", logger, ResultDetails.Create("field", field.Name, "dataSet", draft.Name), FieldsOut, field.Name));
    }

    private static IEnumerable<GraphDataSetProblem> DuplicateComponents(IGraphDataSetConfiguration draft, ILogger logger)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return draft.Components
            .Where(component => !seen.Add(component.Name) && reported.Add(component.Name))
            .Select(component => GraphDataSetProblems.Of("GraphDataSetComponentDuplicate", logger, ResultDetails.Create("component", component.Name, "dataSet", draft.Name), Components, component.Name));
    }

    // Each component of a refused family answers that family's own code, the same families and codes GraphDataSetRefusals.Check answers the first of.
    private static IEnumerable<GraphDataSetProblem> RefusedComponents(IGraphDataSetConfiguration draft, ILogger logger) =>
        draft.Components
            .Select(component => (Component: component, Family: GraphDataSetRefusals.FamilyOf(component.ComponentType)))
            .Where(refused => refused.Family is not null)
            .Select(refused => GraphDataSetProblems.Of(
                refused.Family!.ResultCodeName, logger,
                ResultDetails.Create("component", refused.Component.Name, "dataSet", draft.Name, "kind", refused.Component.ComponentType),
                Components, refused.Component.Name));

    // A bound graph feeds every field out: a field out states the component and the output field that feed it, and the component is in the graph.
    private static IEnumerable<GraphDataSetProblem> UnfedFields(IGraphDataSetConfiguration draft, ILogger logger)
    {
        var names = new HashSet<string>(draft.Components.Select(component => component.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var field in draft.GraphFields)
        {
            if (string.IsNullOrEmpty(field.FedByComponent) != string.IsNullOrEmpty(field.FedByField))
            {
                yield return GraphDataSetProblems.Of("GraphDataSetFieldFedByIncomplete", logger, ResultDetails.Create("field", field.Name, "dataSet", draft.Name), FieldsOut, field.Name);
            }
            else if (string.IsNullOrEmpty(field.FedByComponent))
            {
                yield return GraphDataSetProblems.Of("GraphDataSetFieldOutUnfed", logger, ResultDetails.Create("field", field.Name, "dataSet", draft.Name), FieldsOut, field.Name);
            }
            else if (!names.Contains(field.FedByComponent!))
            {
                yield return GraphDataSetProblems.Of(
                    "GraphDataSetFieldOutFeedMissing", logger,
                    ResultDetails.Create("field", field.Name, "dataSet", draft.Name, "component", field.FedByComponent),
                    FieldsOut, field.Name);
            }
        }
    }

    // The data sets the draft reads or intends to read, and the data stores it names, each asked once for the whole draft. The draft's own name is not asked: the
    // draft replaces whatever is stored under it, and a component that reads it is for the cycle to name.
    private static async Task<IGenericResult<IReadOnlySet<string>>> ReadSources(
        IGraphDataSetConfiguration draft, IGraphDataSetSources sources, Dictionary<string, GraphDataSetSourceFacts> facts, CancellationToken cancellationToken)
    {
        var dataSets = draft.Components.Select(component => component.DataSetName)
            .Concat(draft.Intents.Where(intent => NamesDataSet(intent)).Select(intent => intent.SourceDataSetName))
            .Where(name => !string.IsNullOrEmpty(name) && !string.Equals(name, draft.Name, StringComparison.OrdinalIgnoreCase))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var read = await sources.DataSets(dataSets, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read.ToNewResult<IReadOnlySet<string>>();
        }

        foreach (var entry in read.Value)
        {
            facts[entry.Key] = entry.Value;
        }

        return await sources.DataStores(
            [.. draft.Components.Select(component => component.DataStoreName)
                .Concat(draft.Intents.Where(intent => NamesDataStore(intent)).Select(intent => intent.DataStoreName))
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)],
            cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<GraphDataSetProblem> MissingSources(
        IGraphDataSetConfiguration draft, Dictionary<string, GraphDataSetSourceFacts> facts, IReadOnlySet<string> stores, ILogger logger)
    {
        foreach (var component in draft.Components)
        {
            if (ReadsOtherDataSet(draft, component) && !facts[component.DataSetName!].Exists)
            {
                yield return GraphDataSetProblems.Of(
                    "GraphDataSetSourceMissing", logger,
                    ResultDetails.Create("component", component.Name, "dataSet", draft.Name, "sourceKind", "data set").With("source", component.DataSetName),
                    Components, component.Name);
            }

            if (!string.IsNullOrEmpty(component.DataStoreName) && !stores.Contains(component.DataStoreName))
            {
                yield return GraphDataSetProblems.Of(
                    "GraphDataSetSourceMissing", logger,
                    ResultDetails.Create("component", component.Name, "dataSet", draft.Name, "sourceKind", "data store").With("source", component.DataStoreName),
                    Components, component.Name);
            }
        }
    }

    // The components that name a container of a data store that exists. A data store that does not exist is reported apart (MissingSources), and a component that
    // names a container and no data store is the engine's to refuse.
    private static IEnumerable<GraphDataSetComponentConfiguration> NamedContainers(IGraphDataSetConfiguration draft, IReadOnlySet<string> stores) =>
        draft.Components.Where(component => !string.IsNullOrEmpty(component.ContainerName) && !string.IsNullOrEmpty(component.DataStoreName) && stores.Contains(component.DataStoreName));

    // Where each data store holds the containers the draft names, one read of the store for all of them (FDW-5408).
    private static async Task<IGenericResult<Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>>> ReadContainers(
        IGraphDataSetConfiguration draft, IGraphDataSetSources sources, IReadOnlySet<string> stores, CancellationToken cancellationToken)
    {
        var found = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var store in NamedContainers(draft, stores).GroupBy(component => component.DataStoreName!, StringComparer.OrdinalIgnoreCase))
        {
            var read = await sources.Containers(store.Key, [.. store.Select(component => component.ContainerName!)], cancellationToken).ConfigureAwait(false);
            if (read.IsFailure || read.Value is null)
            {
                return read.ToNewResult<Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>>();
            }

            found[store.Key] = read.Value;
        }

        return GenericResult<Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>>.Success(found);
    }

    // A container the data store does not hold, one it holds only in paths other than the component states, and one it holds in more than one path when the component states none: three reasons, three codes.
    private static IEnumerable<GraphDataSetProblem> ContainerProblems(
        IGraphDataSetConfiguration draft, Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> found, IReadOnlySet<string> stores, ILogger logger)
    {
        foreach (var component in NamedContainers(draft, stores))
        {
            var details = ResultDetails.Create("component", component.Name, "dataSet", draft.Name, "container", component.ContainerName).With("dataStore", component.DataStoreName);
            if (!found[component.DataStoreName!].TryGetValue(component.ContainerName!, out var paths))
            {
                yield return GraphDataSetProblems.Of("GraphDataSetContainerNotFound", logger, details, Components, component.Name);
            }
            else if (!string.IsNullOrEmpty(component.PathName) && !paths.Contains(component.PathName, StringComparer.OrdinalIgnoreCase))
            {
                yield return GraphDataSetProblems.Of("GraphDataSetContainerInOtherPath", logger, details.With("path", component.PathName).With("foundPath", string.Join(", ", paths)), Components, component.Name);
            }
            else if (string.IsNullOrEmpty(component.PathName) && paths.Count > 1)
            {
                yield return GraphDataSetProblems.Of("GraphDataSetContainerAmbiguous", logger, details.With("paths", string.Join(", ", paths)), Components, component.Name);
            }
        }
    }

    // Saving states the id of the data set an intended source names, from the data set it finds; the check states it the same way, so the intent's own
    // validator, which asks for the id, can be run over the draft.
    private static void FillIntentIds(IGraphDataSetConfiguration draft, Dictionary<string, GraphDataSetSourceFacts> facts)
    {
        foreach (var intent in draft.Intents.Where(intent => NamesDataSet(intent) && IsOtherDataSet(draft, intent.SourceDataSetName) && facts[intent.SourceDataSetName!].Exists))
        {
            intent.SourceDataSetId = facts[intent.SourceDataSetName!].Id;
        }
    }

    // The rules saving applies to intended sources: a graph that has components states none (binding replaces them with an empty list), and an intended
    // data set is one that exists. An intended data store need not exist: it may be external.
    private static IEnumerable<GraphDataSetProblem> IntentProblems(IGraphDataSetConfiguration draft, Dictionary<string, GraphDataSetSourceFacts> facts, ILogger logger)
    {
        if (draft.Components.Count > 0 && draft.Intents.Count > 0)
        {
            yield return GraphDataSetProblems.Of("GraphDataSetIntentOnBoundGraph", logger, ResultDetails.Create("dataSet", draft.Name), IntendedSources);
        }

        for (var index = 0; index < draft.Intents.Count; index++)
        {
            var intent = draft.Intents[index];
            if (NamesDataSet(intent) && IsOtherDataSet(draft, intent.SourceDataSetName) && !facts[intent.SourceDataSetName!].Exists)
            {
                yield return GraphDataSetProblems.Of(
                    "GraphDataSetIntentSourceMissing", logger,
                    ResultDetails.Create("index", index, "dataSet", draft.Name, "source", intent.SourceDataSetName),
                    IntendedSources, index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    // Only a bound draft is held to this: an abstract draft that reads an abstract data set is accepted, and a draft with no graph reads nothing anyway.
    private static IEnumerable<GraphDataSetProblem> UnboundSources(IGraphDataSetConfiguration draft, Dictionary<string, GraphDataSetSourceFacts> facts, ILogger logger) =>
        draft.Components
            .Where(component => ReadsOtherDataSet(draft, component) && facts[component.DataSetName!].IsUnboundGraph)
            .Select(component => GraphDataSetProblems.Of(
                "GraphDataSetSourceNotBound", logger,
                ResultDetails.Create("component", component.Name, "dataSet", draft.Name, "source", component.DataSetName),
                Components, component.Name));

    // The data sets the draft reads, through each other, as the draft states its own reads now. A cycle is named at the component that reads the data
    // set the cycle goes through first.
    private static async Task<IGenericResult<IReadOnlyList<GraphDataSetProblem>>> Cycle(
        IGraphDataSetConfiguration draft, IGraphDataSetSources sources, Dictionary<string, GraphDataSetSourceFacts> facts, ILogger logger, CancellationToken cancellationToken)
    {
        var found = await GraphDataSetCycles.Find(
            draft.Name,
            [.. draft.Components.Select(component => component.DataSetName).Where(name => !string.IsNullOrEmpty(name)).Select(name => name!)],
            async (names, token) =>
            {
                var unread = names.Where(name => !facts.ContainsKey(name)).ToList();
                if (unread.Count > 0)
                {
                    var read = await sources.DataSets(unread, token).ConfigureAwait(false);
                    if (read.IsFailure || read.Value is null)
                    {
                        return read.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>();
                    }

                    foreach (var entry in read.Value)
                    {
                        facts[entry.Key] = entry.Value;
                    }
                }

                return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Success(
                    names.ToDictionary(name => name, name => facts[name].Reads, StringComparer.OrdinalIgnoreCase));
            },
            cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            return found.ToNewResult<IReadOnlyList<GraphDataSetProblem>>();
        }

        // Why First: the cycle starts at the draft and goes to a data set the draft reads, and the draft's reads are its components' data set names.
        return GenericResult<IReadOnlyList<GraphDataSetProblem>>.Success(
            found.Value.HasCycle
                ? [GraphDataSetProblems.Of(
                    "GraphDataSetCycle", logger,
                    ResultDetails.Create("dataSet", draft.Name, "cycle", string.Join(" -> ", found.Value.Cycle)),
                    Components, draft.Components.First(component => string.Equals(component.DataSetName, found.Value.Cycle[1], StringComparison.OrdinalIgnoreCase)).Name)]
                : []);
    }

    // Each intended source, as not bound yet. An intent that names nothing its kind names is the intent validator's refusal, and an intended data set that does not exist is a problem, not an entry here.
    private static IReadOnlyList<GraphDataSetNotBoundSource> NotBound(
        IGraphDataSetConfiguration draft, Dictionary<string, GraphDataSetSourceFacts> facts, IReadOnlySet<string> stores) =>
    [
        .. draft.Intents
            .Select((intent, index) => (Intent: intent, Index: index))
            .Where(entry => (NamesDataStore(entry.Intent) && !string.IsNullOrEmpty(entry.Intent.DataStoreName))
                || (NamesDataSet(entry.Intent) && !string.IsNullOrEmpty(entry.Intent.SourceDataSetName)
                    && (!IsOtherDataSet(draft, entry.Intent.SourceDataSetName) || facts[entry.Intent.SourceDataSetName!].Exists)))
            .Select(entry => NamesDataStore(entry.Intent)
                ? new GraphDataSetNotBoundSource([IntendedSources, entry.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)], entry.Intent.IntentKind, entry.Intent.DataStoreName!, stores.Contains(entry.Intent.DataStoreName!))
                : new GraphDataSetNotBoundSource(
                    [IntendedSources, entry.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)],
                    entry.Intent.IntentKind,
                    entry.Intent.SourceDataSetName!,
                    string.Equals(entry.Intent.SourceDataSetName, draft.Name, StringComparison.OrdinalIgnoreCase) || facts[entry.Intent.SourceDataSetName!].Exists)),
    ];

    private static bool NamesDataSet(GraphDataSetIntentConfiguration intent) => GraphIntentKinds.ByName(intent.IntentKind).NamesDataSet;

    private static bool NamesDataStore(GraphDataSetIntentConfiguration intent) => GraphIntentKinds.ByName(intent.IntentKind).NamesDataStore;

    // A component that names a data set other than the draft itself: the draft is not stored yet, so reading it is for the cycle to name.
    private static bool IsOtherDataSet(IGraphDataSetConfiguration draft, string? name) =>
        !string.IsNullOrEmpty(name) && !string.Equals(name, draft.Name, StringComparison.OrdinalIgnoreCase);

    private static bool ReadsOtherDataSet(IGraphDataSetConfiguration draft, GraphDataSetComponentConfiguration component) =>
        !string.IsNullOrEmpty(component.DataSetName) && !string.Equals(component.DataSetName, draft.Name, StringComparison.OrdinalIgnoreCase);
}
