using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// The outputs a graph run gives back (FDW-6136): each output the graph declares, read from the variable that holds it when the run ends, checked against its stated data type and written as
/// invariant text. A value that does not read as its type fails the run; nothing is coerced.
/// </summary>
public static class GraphRunOutputs
{
    /// <summary>Collects the outputs of a run that ended.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="variables">The run's variables, as the run left them.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The outputs in the order the graph declares them (none when it declares none), or the failure naming the output whose value is not of its type.</returns>
    public static IGenericResult<IReadOnlyList<PipelineOutputValue>> Collect(IGraphPipelineConfiguration graph, FlowVariables variables, ILogger logger)
    {
        var values = new List<PipelineOutputValue>();
        foreach (var output in graph.Outputs)
        {
            var type = DataTypes.ByName(output.DataType);
            if (ReferenceEquals(type, DataTypes.NotFound) || !variables.TryGet(output.VariableName, out var held))
            {
                return GenericResult<IReadOnlyList<PipelineOutputValue>>.Failure(PipelineOutputLog.OutputInvalid(logger, graph.Name, output.Name, output.DataType));
            }

            if (held is null)
            {
                values.Add(new PipelineOutputValue(output.Name, output.DataType, null));
                continue;
            }

            var converted = type.Convert(held);
            if (converted.IsFailure || converted.Value is null)
            {
                return GenericResult<IReadOnlyList<PipelineOutputValue>>.Failure(PipelineOutputLog.OutputInvalid(logger, graph.Name, output.Name, output.DataType));
            }

            values.Add(new PipelineOutputValue(output.Name, output.DataType, type.Format(converted.Value)));
        }

        return GenericResult<IReadOnlyList<PipelineOutputValue>>.Success(values);
    }

    /// <summary>Reads a recorded output back as the typed value its stated type holds.</summary>
    /// <param name="output">The recorded output.</param>
    /// <param name="value">The typed value; null for an output recorded with no value.</param>
    /// <returns>Whether the text reads as the stated type.</returns>
    public static bool TryRead(PipelineOutputValue output, out object? value) => GraphValues.TryRead(output.DataType, output.Text, out value);
}
