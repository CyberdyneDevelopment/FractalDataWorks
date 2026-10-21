using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A component's output node: where rows leave through every link that starts at it (FDW-1386).
/// </summary>
public interface IFlowOutput
{
    /// <summary>Gets the name of the output node.</summary>
    string Name { get; }

    /// <summary>
    /// Writes a batch to every link of this output. The write waits while a receiving link is full, which is the back-pressure
    /// that keeps a fast source from outrunning a slow destination.
    /// </summary>
    /// <param name="batch">The rows to send.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>A task that completes when every link has taken the batch.</returns>
    Task Write(FlowBatch batch, CancellationToken cancellationToken = default);
}
