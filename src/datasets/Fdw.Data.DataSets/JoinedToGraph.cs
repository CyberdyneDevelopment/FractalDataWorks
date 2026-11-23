using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The Graph data set a Compound or Federated data set converts to (FDW-6973): each source a Source component, each join a MergeJoin, a union a
/// UnionAll, each source's mappings and transformation chains the input mappings of the input it feeds; every part with no Graph place refused by
/// name.
/// </summary>
/// <remarks>
/// Identity is carried as <see cref="SimpleToGraph"/> carries it. Joins run in their order: the first joins its two sources, each later join takes
/// the stream so far on its left and a new source on its right. A Right join is the Left join of its sides turned round (field names, not
/// positions, carry the fields out). Each right side's fields are prefixed with its source's name and an underscore, so no name collides and none
/// is renamed by a guess. A Federated data set's strategy and its own option values become the Graph data set's (FDW-6901, FDW-6970), and its
/// filters one Filter component after the last join or the union, by the names the fields carry there (<see cref="GraphFilters"/>). Refused: a
/// Cross join or a join type MergeJoin cannot state, a join whose sides are not the stream so far and a new source, and what SimpleToGraph refuses
/// for the fields, filters and key fields.
/// </remarks>
public static class JoinedToGraph
{
    private const string SourceOutput = "output";
    private const string MappingsSetting = "mappings";
    private const string UnionComponent = "union";

    // JoinTypes name -> MergeJoin JoinKinds name, and whether the sides are turned round.
    private static readonly Dictionary<string, (string Kind, bool Turned)> JoinKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Inner"] = ("Inner", false),
        ["Left"] = ("LeftOuter", false),
        ["Right"] = ("LeftOuter", true),
        ["Full"] = ("FullOuter", false),
    };

    /// <summary>Builds the Graph data set a Compound or Federated data set converts to.</summary>
    /// <param name="stored">The data set as it is stored.</param>
    /// <param name="batchSize">How many rows each component reads or joins in a batch: the caller's choice, at least one.</param>
    /// <param name="chains">Each source's stored chain links, by source name and then by logical field, in chain order.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The Graph configuration, the data set's own combination refusal (FDW-6946), or <c>DataSetConversionRefused</c> naming every part.</returns>
    public static IGenericResult<GraphDataSetConfiguration> Convert(
        IJoinedDataSetConfiguration stored,
        int batchSize,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>> chains,
        ILogger logger)
    {
        var combined = SourceCombinationRules.Validate(stored, logger);
        if (combined.IsFailure)
            return combined.ToNewResult<GraphDataSetConfiguration>();

        var refused = Refusals(stored, batchSize);
        var graph = new GraphDataSetConfiguration();
        // A Federated data set keeps the strategy it states (FDW-6970): a converted data set reads across its stores as it did; one that states none keeps the FDW-level default.
        graph.FederationStrategy = stored.FederationStrategyOrNone();
        var sources = stored.Sources.ToDictionary(source => source.SourceName, StringComparer.OrdinalIgnoreCase);
        var components = sources.Values.Select(source => SourceComponent(source, batchSize)).ToList();
        var links = new List<GraphDataSetLinkConfiguration>();

        // The name each logical field carries in the stream the graph's fields come out of.
        var stream = new Dictionary<string, string>(StringComparer.Ordinal);
        string? last;
        if (SourceCombinationRules.IsUnion(stored))
        {
            var union = Component(UnionComponent, "UnionAll", batchSize);
            foreach (var source in stored.Sources)
            {
                links.Add(Link(source.SourceName, UnionComponent, source.SourceName));
                union.Fields = [.. union.Fields, .. InputMappings(source, source.SourceName, stored.Fields, chains)];
            }

            components.Add(union);
            foreach (var field in Stored(stored.Fields))
                stream[field.Name] = field.Name;
            last = UnionComponent;
        }
        else
        {
            last = Joins(stored, sources, chains, batchSize, components, links, stream, refused);
        }

        refused.AddRange(Stored(stored.Fields).Where(field => !stream.ContainsKey(field.Name)).Select(field => $"field '{field.Name}' (no source maps it)"));
        last = Filtered(stored, stream, components, links, refused, last);

        if (refused.Count > 0 || last is null)
            return GenericResult<GraphDataSetConfiguration>.Failure(
                DataSetsResultCodes.ByName("DataSetConversionRefused"), logger,
                ResultDetails.Create("name", stored.Name, "parts", string.Join("; ", refused)));

        var carried = DataSetKindMove.Carried(stored, graph, GraphDataSetConfiguration.ImplementationName);
        if (carried.IsFailure)
            return carried.ToNewResult<GraphDataSetConfiguration>();

        graph.Components = components;
        graph.Links = links;
        graph.FederationOptions = [.. stored.FederationOptionsOrNone()];
        graph.FieldsOutComponent = last;
        graph.FieldsOutOutput = SourceOutput;
        var keys = stored.KeyFields.Select(key => key.KeyName).ToHashSet(StringComparer.Ordinal);
        foreach (var field in graph.GraphFields)
        {
            field.IsKey = field.IsKey || keys.Contains(field.Name);
            if (field.CalculationName is not null)
                continue;
            field.FedByComponent = last;
            field.FedByField = stream[field.Name];
        }

        return GenericResult<GraphDataSetConfiguration>.Success(graph);
    }

    // The data set's filters as a Filter component after the last one, which it then is; the last component unchanged when there is none.
    private static string? Filtered(
        IJoinedDataSetConfiguration stored, Dictionary<string, string> stream, List<GraphDataSetComponentConfiguration> components,
        List<GraphDataSetLinkConfiguration> links, List<string> refused, string? last)
    {
        if (last is null || GraphFilters.Component(stored.Filters, stream, [.. components.Select(component => component.Name)], refused) is not { } filter)
            return last;

        components.Add(filter);
        links.Add(Link(last, filter.Name, "input"));
        return filter.Name;
    }

    // What the data set carries that a Graph data set has no place for, before its joins are read.
    private static List<string> Refusals(IJoinedDataSetConfiguration stored, int batchSize)
    {
        var refused = new List<string>();
        if (batchSize < 1)
            refused.Add($"batch size {batchSize} (at least one)");
        refused.AddRange(stored.Sources.Where(source => source.FieldMappings.Count == 0).Select(source => $"source '{source.SourceName}' (it maps no fields, so the graph cannot name them)"));
        refused.AddRange(SimpleToGraph.FieldParts(stored.Fields));
        if (!SimpleToGraph.KeysInFieldOrder(stored))
            refused.Add($"key fields {string.Join(", ", stored.KeyFields.OrderBy(key => key.Ordinal).Select(key => key.KeyName))} in an order other than their fields'");
        return refused;
    }

    // Each join a MergeJoin over the stream so far and one new source; the name of the last, or null when none could be built.
    private static string? Joins(
        IJoinedDataSetConfiguration stored,
        Dictionary<string, DataSetSourceConfiguration> sources,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>> chains,
        int batchSize,
        List<GraphDataSetComponentConfiguration> components,
        List<GraphDataSetLinkConfiguration> links,
        Dictionary<string, string> stream,
        List<string> refused)
    {
        var joined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? last = null;
        foreach (var (join, index) in stored.Joins.OrderBy(j => j.Ordinal).Select((join, index) => (join, index)))
        {
            if (!JoinKinds.TryGetValue(join.JoinType, out var kind))
            {
                refused.Add($"join {join.Ordinal} of type '{join.JoinType}' (a MergeJoin states Inner, Left, Right or Full)");
                continue;
            }

            // The stream so far is the left of every join after the first; the source a join brings in is its right.
            var first = last is null;
            string? newSource = null;
            if (!first && !BringsOneNewSource(join, joined, out newSource))
            {
                refused.Add($"join {join.Ordinal} between '{join.LeftSourceName}' and '{join.RightSourceName}' (it does not bring one new source to the sources joined before it)");
                continue;
            }

            var name = $"join{index + 1}";
            var merge = Component(name, "MergeJoin", batchSize);
            var (leftSource, leftField, rightSource, rightField) = kind.Turned
                ? (join.RightSourceName, join.RightFieldName, join.LeftSourceName, join.LeftFieldName)
                : (join.LeftSourceName, join.LeftFieldName, join.RightSourceName, join.RightFieldName);
            if (!first && !string.Equals(rightSource, newSource, StringComparison.OrdinalIgnoreCase))
            {
                refused.Add($"join {join.Ordinal} of type '{join.JoinType}' (its turned sides would put the sources joined before it on the right)");
                continue;
            }

            merge.JoinKind = kind.Kind;
            merge.RightPrefix = $"{rightSource}_";
            merge.Columns = [Column("leftKeys", first ? leftField : StreamName(stream, leftField)), Column("rightKeys", rightField)];

            if (first)
            {
                links.Add(Link(leftSource, name, "left"));
                merge.Fields = [.. InputMappings(sources[leftSource], "left", stored.Fields, chains)];
                foreach (var field in MappedFields(sources[leftSource]))
                    stream[field] = field;
                joined.Add(leftSource);
            }
            else
            {
                links.Add(Link(last!, name, "left"));
            }

            links.Add(Link(rightSource, name, "right"));
            merge.Fields = [.. merge.Fields, .. InputMappings(sources[rightSource], "right", stored.Fields, chains)];
            foreach (var field in MappedFields(sources[rightSource]).Where(field => !stream.ContainsKey(field)))
                stream[field] = merge.RightPrefix + field;
            joined.Add(rightSource);
            components.Add(merge);
            last = name;
        }

        foreach (var unjoined in sources.Keys.Where(source => !joined.Contains(source)))
            refused.Add($"source '{unjoined}' (no join brings it in)");
        return last;
    }

    // Whether a later join has one side among the sources joined so far and brings in one source not yet joined, which it names.
    private static bool BringsOneNewSource(DataSetJoinConfiguration join, HashSet<string> joined, out string? newSource)
    {
        var leftJoined = joined.Contains(join.LeftSourceName);
        var rightJoined = joined.Contains(join.RightSourceName);
        newSource = leftJoined && !rightJoined ? join.RightSourceName : rightJoined && !leftJoined ? join.LeftSourceName : null;
        return newSource is not null;
    }

    private static string StreamName(Dictionary<string, string> stream, string field) => stream.TryGetValue(field, out var name) ? name : field;

    // The logical fields a source carries into the graph: those its mappings name.
    private static IEnumerable<string> MappedFields(DataSetSourceConfiguration source) => source.FieldMappings.Keys;

    // A source's mappings as the input mappings of the input it feeds: each logical field from its column, with its stored chain.
    private static IEnumerable<GraphDataSetComponentFieldConfiguration> InputMappings(
        DataSetSourceConfiguration source,
        string input,
        IEnumerable<DataSetFieldConfiguration> fields,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>> chains)
    {
        var order = fields.Select((field, index) => (field.Name, index)).ToDictionary(pair => pair.Name, pair => pair.index, StringComparer.Ordinal);
        var links = chains.TryGetValue(source.SourceName, out var bySource) ? bySource : new Dictionary<string, IReadOnlyList<AttachedFieldTransformation>>(StringComparer.Ordinal);
        return source.FieldMappings
            .OrderBy(mapping => order.TryGetValue(mapping.Key, out var at) ? at : int.MaxValue)
            .Select((mapping, ordinal) => new GraphDataSetComponentFieldConfiguration
            {
                Id = Guid.CreateVersion7(),
                SettingName = MappingsSetting,
                InputName = input,
                Ordinal = ordinal,
                TargetField = mapping.Key,
                SourceField = mapping.Value,
                TransformationChain = links.TryGetValue(mapping.Key, out var chain)
                    ? [.. chain.Select(link => new GraphDataSetComponentFieldTransformationConfiguration
                    {
                        Id = Guid.CreateVersion7(), Name = link.Name, TransformationId = link.TransformationId, Ordinal = link.Ordinal,
                    })]
                    : [],
            });
    }

    private static IEnumerable<DataSetFieldConfiguration> Stored(IEnumerable<DataSetFieldConfiguration> fields) =>
        fields.Where(f => f.IsCurrent && !f.IsDeleted && f.CalculationName is null);

    private static GraphDataSetComponentConfiguration SourceComponent(DataSetSourceConfiguration source, int batchSize)
    {
        var component = Component(source.SourceName, string.Equals(source.SourceKind, "DataSet", StringComparison.Ordinal) ? "DataSetSource" : "ContainerSource", batchSize);
        component.DataSetName = source.SourceDataSetName;
        if (!string.Equals(component.ComponentType, "DataSetSource", StringComparison.Ordinal))
        {
            component.DataSetName = null;
            component.DataStoreName = source.DataStoreName;
            component.PathName = source.PathValue;
            component.ContainerName = source.ContainerName;
        }

        return component;
    }

    private static GraphDataSetComponentConfiguration Component(string name, string type, int batchSize) =>
        new() { Id = Guid.CreateVersion7(), Name = name, ComponentType = type, BatchSize = batchSize };

    private static GraphDataSetComponentColumnConfiguration Column(string setting, string column) =>
        new() { Id = Guid.CreateVersion7(), SettingName = setting, Ordinal = 0, ColumnName = column };

    private static GraphDataSetLinkConfiguration Link(string from, string to, string input) =>
        new() { Id = Guid.CreateVersion7(), FromComponent = from, FromOutput = SourceOutput, ToComponent = to, ToInput = input };
}
