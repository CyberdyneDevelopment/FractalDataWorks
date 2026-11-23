using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Gathers what each map edge contributes and turns it into the lineage's steps.</summary>
internal sealed class LineageFlowCollector : ILineageFlowSink
{
    private readonly Dictionary<string, List<string>> _reads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _writes = new(StringComparer.Ordinal);
    private readonly List<LineageFlow> _flows = [];

    /// <inheritdoc />
    public void Read(string pipelineId, string inputId) => Add(_reads, pipelineId, inputId);

    /// <inheritdoc />
    public void Write(string pipelineId, string outputId) => Add(_writes, pipelineId, outputId);

    /// <inheritdoc />
    public void Step(MapEdgeDto edge, string from, string to, string relation)
        => _flows.Add(new LineageFlow(from, to, relation, null, IntendedOf(edge), GraphOf(edge)));

    /// <summary>Gets the steps: those the edges are, then a step for each input and output pair of each pipeline.</summary>
    /// <returns>The steps.</returns>
    /// <remarks>A pipeline that reads several inputs and writes several outputs gives a step for each input and output pair.</remarks>
    public List<LineageFlow> Flows()
    {
        var flows = new List<LineageFlow>(_flows);
        foreach (var (pipelineId, inputs) in _reads)
        {
            if (!_writes.TryGetValue(pipelineId, out var outputs)) continue;

            foreach (var input in inputs)
            {
                foreach (var output in outputs)
                    flows.Add(new LineageFlow(input, output, LineageRelations.Pipeline, pipelineId));
            }
        }

        return flows;
    }

    // An edge a Graph data set draws says so in its metadata (FDW-5456).
    private static bool GraphOf(MapEdgeDto edge)
        => edge.Metadata.TryGetValue(MapEdges.GraphKey, out var value) && value is true;

    // An edge a Graph data set draws for a source it only intends to read says so in its metadata (FDW-5296).
    private static bool IntendedOf(MapEdgeDto edge)
        => edge.Metadata.TryGetValue(MapEdges.IntendedKey, out var value) && value is true;

    private static void Add(Dictionary<string, List<string>> index, string key, string value)
    {
        if (!index.TryGetValue(key, out var values))
        {
            values = [];
            index[key] = values;
        }

        values.Add(value);
    }
}
