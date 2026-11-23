using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The Graph data set a Simple data set converts to (FDW-6972): the same identity and fields, its one source as a Source component that feeds
/// every field out, and every part a Graph data set has no place for refused by name, never dropped.
/// </summary>
/// <remarks>
/// The identity is carried the way the move between kinds carries it (<see cref="DataSetKindMove"/>): Id, name, fields with their Ids, category,
/// classification, owner, version, caching and audit trail. The provider's write of the result retires the Simple row in the same unit (FDW-6806).
/// The table of what lands where is in FDW-6972's plan. Its filters become one Filter component after the source (<see cref="GraphFilters"/>).
/// Refused: aggregates, a filter the condition language cannot state, key fields whose order is not their fields' order, a field's default value or container binding, a mapping that carries a transformation chain, and a
/// data set with no source.
/// </remarks>
public static class SimpleToGraph
{
    /// <summary>The output of a Source component (ContainerSource, DataSetSource) that carries every row it reads.</summary>
    private const string SourceOutput = "output";

    /// <summary>The source kind of a source that is another data set.</summary>
    private const string DataSetSourceKind = "DataSet";

    /// <summary>Builds the Graph data set a data set converts to.</summary>
    /// <param name="stored">The data set as it is stored.</param>
    /// <param name="batchSize">How many rows the Source component reads in a batch: the caller's choice, at least one.</param>
    /// <param name="chainedFields">The fields whose mapping carries a transformation chain, read from the stored chains.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The Graph configuration, or the refusal: <c>DataSetAlreadyGraph</c> for a Graph data set, <c>DataSetConversionRefused</c> naming every part
    /// that has no place in a Graph data set (and a Compound or Federated data set, whose conversion is FDW-6973's).
    /// </returns>
    public static IGenericResult<GraphDataSetConfiguration> Convert(
        IDataSetImplementationConfiguration stored, int batchSize, IReadOnlyCollection<string> chainedFields, ILogger logger)
    {
        if (stored is IGraphDataSetConfiguration)
            return GenericResult<GraphDataSetConfiguration>.Failure(
                DataSetsResultCodes.ByName("DataSetAlreadyGraph"), logger, ResultDetails.Create("name", stored.Name));

        var refused = Refusals(stored, batchSize, chainedFields);
        var source = stored.Sources.Count == 1 ? stored.Sources[0] : null;
        var stream = Stream(stored, source);
        var filter = GraphFilters.Component(stored.Filters, stream, [.. stored.Sources.Select(s => s.SourceName)], refused);
        if (refused.Count > 0 || source is null)
            return GenericResult<GraphDataSetConfiguration>.Failure(
                DataSetsResultCodes.ByName("DataSetConversionRefused"), logger,
                ResultDetails.Create("name", stored.Name, "parts", string.Join("; ", refused)));

        var graph = new GraphDataSetConfiguration();
        var carried = DataSetKindMove.Carried(stored, graph, GraphDataSetConfiguration.ImplementationName);
        if (carried.IsFailure)
            return carried.ToNewResult<GraphDataSetConfiguration>();

        var component = Component(source, batchSize);
        graph.Components = filter is null ? [component] : [component, filter];
        graph.Links = filter is null ? [] : [Link(component.Name, filter.Name)];
        var last = filter is null ? component.Name : filter.Name;
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

    // Every part the data set carries that a Graph data set has no place for, each named; empty when it converts whole.
    private static List<string> Refusals(IDataSetImplementationConfiguration stored, int batchSize, IReadOnlyCollection<string> chainedFields)
    {
        var refused = new List<string>();
        if (stored is not ISimpleDataSetConfiguration)
        {
            refused.Add($"its kind {stored.Implementation}");
            return refused;
        }

        if (stored.Sources.Count != 1)
            refused.Add($"{stored.Sources.Count} sources (a Simple data set converts from exactly one)");
        if (batchSize < 1)
            refused.Add($"batch size {batchSize} (at least one)");

        refused.AddRange(stored.AggregatesOrNone().Select(aggregate => $"aggregate '{aggregate.Name}'"));
        refused.AddRange(chainedFields.Select(field => $"the transformation chain of field '{field}'"));
        refused.AddRange(FieldParts(stored.Fields));

        if (!KeysInFieldOrder(stored))
            refused.Add($"key fields {string.Join(", ", stored.KeyFields.OrderBy(key => key.Ordinal).Select(key => key.KeyName))} in an order other than their fields'");

        return refused;
    }

    // A field's members a Graph field has no place for.
    internal static IEnumerable<string> FieldParts(IEnumerable<DataSetFieldConfiguration> fields)
    {
        foreach (var field in fields.Where(f => f.IsCurrent && !f.IsDeleted))
        {
            if (field.DefaultValue is not null)
                yield return $"the default value of field '{field.Name}'";
            if (field.BoundContainerFieldId is not null || field.BindingKind is not null || field.BindingConfidence is not null)
                yield return $"the container binding of field '{field.Name}'";
            if (field.Calculator is not null && field.CalculationName is null)
                yield return $"the unnamed calculation of field '{field.Name}'";
        }
    }

    // A Graph data set's key is the fields marked IsKey, in field order: the stored key order must be that order.
    internal static bool KeysInFieldOrder(IDataSetImplementationConfiguration stored)
    {
        var fieldOrder = stored.Fields.Where(f => f.IsCurrent && !f.IsDeleted).OrderBy(f => f.Ordinal).Select(f => f.Name).ToList();
        var keyOrder = stored.KeyFields.OrderBy(key => key.Ordinal).Select(key => key.KeyName).ToList();
        return keyOrder.SequenceEqual(fieldOrder.Where(name => keyOrder.Contains(name, StringComparer.Ordinal)), StringComparer.Ordinal);
    }

    // The name each stored field carries in the source's stream: the column its mapping names, or its own name when it has no mapping (as the
    // Simple read does); empty when there is no one source.
    private static Dictionary<string, string> Stream(IDataSetImplementationConfiguration stored, DataSetSourceConfiguration? source) =>
        source is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : stored.Fields.Where(field => !field.IsCalculated).ToDictionary(
                field => field.Name, field => source.FieldMappings.TryGetValue(field.Name, out var column) ? column : field.Name, StringComparer.Ordinal);

    // A link from a component's output to a stage tool's input.
    private static GraphDataSetLinkConfiguration Link(string from, string to) =>
        new() { Id = Guid.CreateVersion7(), FromComponent = from, FromOutput = SourceOutput, ToComponent = to, ToInput = "input" };

    // The Source component that reads the data set's one source: a ContainerSource for a container, a DataSetSource for another data set.
    private static GraphDataSetComponentConfiguration Component(DataSetSourceConfiguration source, int batchSize) =>
        string.Equals(source.SourceKind, DataSetSourceKind, StringComparison.Ordinal)
            ? new GraphDataSetComponentConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = source.SourceName,
                ComponentType = "DataSetSource",
                DataSetName = source.SourceDataSetName,
                BatchSize = batchSize,
            }
            : new GraphDataSetComponentConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = source.SourceName,
                ComponentType = "ContainerSource",
                DataStoreName = source.DataStoreName,
                PathName = source.PathValue,
                ContainerName = source.ContainerName,
                BatchSize = batchSize,
            };
}
