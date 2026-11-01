using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A union all: takes the rows of several input nodes and sends them out through one output node (FDW-1386). SSIS Union All.
/// </summary>
/// <remarks>
/// <para>
/// Rows leave in the order they arrive across the inputs, which is not a promised order between inputs; within one input the
/// order is kept. An input that maps its fields (FDW-2013) hands rows over already mapped, like the input of any component.
/// </para>
/// <para>
/// A component that needs several inputs does not need this one when the links of one input node already merge: several links
/// into one node are a union with the same columns. Use this component when the inputs need different column mappings.
/// </para>
/// </remarks>
public sealed class FlowUnionAll : IFlowComponent
{
    /// <summary>The name of the component's output node.</summary>
    public const string OutputName = "output";

    /// <summary>Initializes a new instance of the <see cref="FlowUnionAll"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="inputs">The input nodes' names; at least two.</param>
    public FlowUnionAll(string name, IReadOnlyList<string> inputs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(inputs);
        if (inputs.Count < 2)
        {
            throw new ArgumentException("A union all has at least two inputs.", nameof(inputs));
        }

        if (inputs.Distinct(StringComparer.OrdinalIgnoreCase).Count() != inputs.Count)
        {
            throw new ArgumentException("A union all's input names are all different.", nameof(inputs));
        }

        Name = name;
        Inputs = inputs;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [OutputName];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var output = ports.Output(OutputName);
        var results = await Task.WhenAll(Inputs.Select(input => Forward(ports.Input(input), output, cancellationToken)))
            .ConfigureAwait(false);
        return results.FirstOrDefault(r => r.IsFailure) is { } failed ? failed : GenericResult.Success();
    }

    private static async Task<IGenericResult> Forward(IFlowInput input, IFlowOutput output, CancellationToken cancellationToken)
    {
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            await output.Write(batch, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult.Success();
    }
}
