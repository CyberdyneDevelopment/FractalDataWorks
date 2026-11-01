using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>Reads the thresholds on waiting runs the host is configured with (FDW-3161).</summary>
public interface IWaitThresholdSource
{
    /// <summary>Reads what the server, every pipeline and every schedule state.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration; a failure naming what is missing or stated wrongly, with nothing assumed.</returns>
    Task<IGenericResult<WaitThresholdConfiguration>> Read(CancellationToken cancellationToken = default);
}
