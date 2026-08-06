using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Service for computing statistical summaries on data columns.
/// </summary>
public interface IStatSetService
{
    /// <summary>
    /// Computes statistical summaries for the specified columns.
    /// </summary>
    /// <param name="request">The stat set request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed statistics per column.</returns>
    Task<IGenericResult<StatSetResponse>> ComputeStatSet(
        StatSetRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes statistical summaries grouped by the specified columns.
    /// </summary>
    /// <param name="request">The grouped stat set request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed grouped statistics.</returns>
    Task<IGenericResult<GroupedStatSetResponse>> ComputeGroupedStatSet(
        GroupedStatSetRequest request,
        CancellationToken cancellationToken = default);
}
