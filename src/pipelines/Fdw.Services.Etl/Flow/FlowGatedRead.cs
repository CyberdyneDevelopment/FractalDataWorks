using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A read that feeds a join, lookup or union across data stores, let run by the read gate of the federation strategy (FDW-6970). It is the read it
/// wraps, under the same name and outputs; only when it starts differs.
/// </summary>
public sealed class FlowGatedRead : IFlowComponent
{
    private readonly IFlowComponent _read;
    private readonly FlowReadGate _gate;
    private readonly bool _buildSide;

    /// <summary>Initializes a new instance of the <see cref="FlowGatedRead"/> class.</summary>
    /// <param name="read">The read.</param>
    /// <param name="gate">The gate it waits at.</param>
    /// <param name="buildSide">Whether it feeds a join's build side.</param>
    public FlowGatedRead(IFlowComponent read, FlowReadGate gate, bool buildSide)
    {
        _read = read;
        _gate = gate;
        _buildSide = buildSide;
    }

    /// <inheritdoc />
    public string Name => _read.Name;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => _read.Inputs;

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => _read.Outputs;

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var slot = await _gate.Enter(_buildSide, cancellationToken).ConfigureAwait(false);
        await using (slot.ConfigureAwait(false))
        {
            return await _read.Run(ports, cancellationToken).ConfigureAwait(false);
        }
    }
}
