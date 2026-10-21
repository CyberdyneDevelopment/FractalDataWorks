using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A source component: reads rows from a provider and sends them out in batches (FDW-1386). It has no input node.
/// </summary>
/// <remarks>
/// The provider is what reads a data set, a data store container or a stream; this component only batches what it yields and
/// writes it to its one output node, <c>output</c>. The data gateway stays behind the provider.
/// </remarks>
public sealed class FlowSource : IFlowComponent
{
    /// <summary>The name of the source's one output node.</summary>
    public const string OutputName = "output";

    private readonly Func<CancellationToken, IAsyncEnumerable<IDictionary<string, object?>>> _rows;
    private readonly int _batchSize;

    /// <summary>Initializes a new instance of the <see cref="FlowSource"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="rows">Yields the rows to send.</param>
    /// <param name="batchSize">How many rows go in a batch; at least one.</param>
    public FlowSource(string name, Func<CancellationToken, IAsyncEnumerable<IDictionary<string, object?>>> rows, int batchSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        Name = name;
        _rows = rows;
        _batchSize = batchSize;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [OutputName];

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var output = ports.Output(OutputName);
        var batch = new List<IDictionary<string, object?>>(_batchSize);
        await foreach (var row in _rows(cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            batch.Add(row);
            if (batch.Count >= _batchSize)
            {
                await output.Write(new FlowBatch(batch), cancellationToken).ConfigureAwait(false);
                batch = new List<IDictionary<string, object?>>(_batchSize);
            }
        }

        if (batch.Count > 0)
        {
            await output.Write(new FlowBatch(batch), cancellationToken).ConfigureAwait(false);
        }

        return GenericResult.Success();
    }
}
