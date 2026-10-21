using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A component's input node: where rows arrive from every link that ends at it (FDW-1386).
/// </summary>
public interface IFlowInput
{
    /// <summary>Gets the name of the input node.</summary>
    string Name { get; }

    /// <summary>Reads the next batch.</summary>
    /// <param name="cancellationToken">Cancels the wait; it is cancelled when another component of the run fails.</param>
    /// <returns>The next batch, or null once every link into this input has finished and been read.</returns>
    Task<FlowBatch?> Read(CancellationToken cancellationToken = default);
}
