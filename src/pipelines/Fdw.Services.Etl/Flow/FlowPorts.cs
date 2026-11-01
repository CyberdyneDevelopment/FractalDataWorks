using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The nodes of one component in one run (FDW-1386).
/// </summary>
internal sealed class FlowPorts : IFlowPorts
{
    private readonly Dictionary<string, FlowChannelInput> _inputs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FlowChannelOutput> _outputs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds an input node.</summary>
    /// <param name="input">The node.</param>
    public void Add(FlowChannelInput input) => _inputs.Add(input.Name, input);

    /// <summary>Adds an output node.</summary>
    /// <param name="output">The node.</param>
    public void Add(FlowChannelOutput output) => _outputs.Add(output.Name, output);

    /// <inheritdoc />
    public IFlowInput Input(string name) =>
        _inputs.TryGetValue(name, out var input)
            ? input
            : throw new InvalidOperationException($"The component has no input node '{name}'.");

    /// <inheritdoc />
    public IFlowOutput Output(string name) =>
        _outputs.TryGetValue(name, out var output)
            ? output
            : throw new InvalidOperationException($"The component has no output node '{name}'.");

    /// <summary>Tells every receiving input that the component has finished writing.</summary>
    public void FinishOutputs()
    {
        foreach (var output in _outputs.Values)
        {
            output.Finish();
        }
    }

    /// <summary>Reads what the component left unread on every input and discards it.</summary>
    /// <param name="cancellationToken">Stops the drain when the run is cancelled.</param>
    /// <returns>The discarded row count per input name, for the inputs that had any.</returns>
    public async Task<Dictionary<string, long>> DrainInputs(CancellationToken cancellationToken)
    {
        var discarded = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in _inputs.Values)
        {
            var rows = await input.Drain(cancellationToken).ConfigureAwait(false);
            if (rows > 0)
            {
                discarded[input.Name] = rows;
            }
        }

        return discarded;
    }
}
