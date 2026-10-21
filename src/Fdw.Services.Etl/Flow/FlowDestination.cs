using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A destination component: hands each batch it receives to a writer (FDW-1386). It has no output node.
/// </summary>
/// <remarks>
/// The writer is what loads a data set or container (the load steps of a pipeline); this component reads its one input node,
/// <c>input</c>, to the end and stops the run at the first batch the writer refuses.
/// </remarks>
public sealed class FlowDestination : IFlowComponent
{
    /// <summary>The name of the destination's one input node.</summary>
    public const string InputName = "input";

    private readonly Func<FlowBatch, CancellationToken, Task<IGenericResult>> _write;

    /// <summary>Initializes a new instance of the <see cref="FlowDestination"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="write">Writes one batch.</param>
    public FlowDestination(string name, Func<FlowBatch, CancellationToken, Task<IGenericResult>> write)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(write);
        Name = name;
        _write = write;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [InputName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var input = ports.Input(InputName);
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            var written = await _write(batch, cancellationToken).ConfigureAwait(false);
            if (written.IsFailure)
            {
                return written;
            }
        }

        return GenericResult.Success();
    }
}
