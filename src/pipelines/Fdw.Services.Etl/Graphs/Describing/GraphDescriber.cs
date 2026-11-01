using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// Works out the fields a row has at every input and output node of a graph draft (FDW-1397), so the person building the graph
/// can pick fields instead of typing them.
/// </summary>
/// <remarks>
/// <para>
/// Components are taken in the order their links allow, a source first. A node whose fields cannot be known before a run (a
/// container source, anything fed by one) answers <see langword="null"/>, not an empty list: unknown is not the same as no
/// fields. A graph with a cycle describes only the components before it. A data set that cannot be read fails the description.
/// </para>
/// <para>
/// The graph need not be valid: describing is how a half-built graph is drawn, so a node nothing feeds has no fields yet and the
/// components after it are described from what is known.
/// </para>
/// </remarks>
public static class GraphDescriber
{
    /// <summary>Describes the fields at every node of every component of a graph.</summary>
    /// <param name="graph">The stored graph or draft.</param>
    /// <param name="context">What reaches the data sets a source reads.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">Cancels the description.</param>
    /// <returns>The nodes with their fields, a component after those that feed it and a stage after the stages before it; or the failure that stopped a deriver.</returns>
    public static async Task<IGenericResult<IReadOnlyList<GraphNodeFields>>> Describe(
        GraphPipelineConfiguration graph, DescribeContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        var links = (IReadOnlyList<PipelineLinkConfiguration>)[.. graph.Links];
        var produced = new Dictionary<(string Component, string Node), IReadOnlyList<GraphField>?>(new NodeComparer());
        var nodes = new List<GraphNodeFields>();
        var remaining = graph.Components.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        while (remaining.Count > 0)
        {
            var wave = NextWave(remaining, links);
            if (wave.Count == 0)
            {
                break;
            }

            foreach (var component in wave)
            {
                var described = await DescribeComponent(component, links, produced, context, cancellationToken).ConfigureAwait(false);
                if (described.IsFailure || described.Value is null)
                {
                    return described.ToNewResult<IReadOnlyList<GraphNodeFields>>();
                }

                nodes.AddRange(described.Value);
            }

            remaining.RemoveAll(wave.Contains);
        }

        return GenericResult<IReadOnlyList<GraphNodeFields>>.Success(nodes);
    }

    private static async Task<IGenericResult<IReadOnlyList<GraphNodeFields>>> DescribeComponent(
        PipelineComponentConfiguration component,
        IReadOnlyList<PipelineLinkConfiguration> links,
        Dictionary<(string Component, string Node), IReadOnlyList<GraphField>?> produced,
        DescribeContext context,
        CancellationToken cancellationToken)
    {
        var nodes = new List<GraphNodeFields>();
        var (inputNames, outputNames) = ComponentShapes.Of(component, links);
        var inputs = new Dictionary<string, IReadOnlyList<GraphField>?>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputNames)
        {
            var feeding = links.FirstOrDefault(link => Same(link.ToComponent, component.Name) && Same(link.ToInput, input));
            inputs[input] = feeding is not null && produced.TryGetValue((feeding.FromComponent, feeding.FromOutput), out var fields)
                ? fields
                : null;
        }

        // Why once: the mappings are applied (and their problems found) one time, and what the component sees serves both its input nodes and its outputs (FDW-2183).
        var seen = InputMappings.Apply(component, inputs, context);
        foreach (var input in inputNames)
        {
            nodes.Add(new GraphNodeFields(component.Name, input, true, inputs[input], seen[input]));
        }

        var outputs = await Outputs(component, seen, context, cancellationToken).ConfigureAwait(false);
        if (outputs.IsFailure || outputs.Value is null)
        {
            return outputs.ToNewResult<IReadOnlyList<GraphNodeFields>>();
        }

        foreach (var output in outputNames)
        {
            var fields = outputs.Value.TryGetValue(output, out var derived) ? derived : null;
            produced[(component.Name, output)] = fields;
            nodes.Add(new GraphNodeFields(component.Name, output, false, fields, null));
        }

        return GenericResult<IReadOnlyList<GraphNodeFields>>.Success(nodes);
    }

    private static Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>> Outputs(
        PipelineComponentConfiguration component,
        IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> inputs,
        DescribeContext context,
        CancellationToken cancellationToken)
    {
        var deriver = ComponentFieldDerivers.ByName(component.ComponentType);
        return ReferenceEquals(deriver, ComponentFieldDerivers.NotFound)
            ? Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>>(
                GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>.Success(
                    new Dictionary<string, IReadOnlyList<GraphField>?>(StringComparer.OrdinalIgnoreCase)))
            : deriver.Derive(component, inputs, context, cancellationToken);
    }

    // The components that nothing remaining feeds, of the lowest stage among them: a component after every component that feeds it and a stage after the
    // stages before it. Whatever is left over (a cycle) is not described.
    private static List<PipelineComponentConfiguration> NextWave(List<PipelineComponentConfiguration> remaining, IReadOnlyList<PipelineLinkConfiguration> links)
    {
        var ready = remaining
            .Where(component => !links.Any(link =>
                Same(link.ToComponent, component.Name) && remaining.Any(other => Same(other.Name, link.FromComponent) && !ReferenceEquals(other, component))))
            .ToList();
        if (ready.Count == 0)
        {
            return ready;
        }

        var lowest = ready.Min(component => component.Stage ?? 0);
        return [.. ready.Where(component => (component.Stage ?? 0) == lowest)];
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private sealed class NodeComparer : IEqualityComparer<(string Component, string Node)>
    {
        public bool Equals((string Component, string Node) x, (string Component, string Node) y) =>
            Same(x.Component, y.Component) && Same(x.Node, y.Node);

        public int GetHashCode((string Component, string Node) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Component), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Node));
    }
}
