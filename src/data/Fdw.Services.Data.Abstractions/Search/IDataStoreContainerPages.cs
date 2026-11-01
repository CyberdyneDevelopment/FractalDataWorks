using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Reads the containers of all data stores one page at a time (FDW-2311). A page reads the rows of that page and the names above them, so its cost does
/// not grow with the number of containers.
/// </summary>
public interface IDataStoreContainerPages
{
    /// <summary>Reads one page of containers in container-name order.</summary>
    /// <param name="condition">The condition on the container's own columns the page is narrowed by; none keeps every container.</param>
    /// <param name="skip">How many containers to pass.</param>
    /// <param name="take">How many containers the page holds, from 1 to <see cref="MaxTake"/>.</param>
    /// <param name="includeTotal">True to count the containers the condition keeps.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page, or the failure when a read failed or the page is larger than a page may be.</returns>
    Task<IGenericResult<DataStoreContainerPage>> Page(IFilterNode? condition, int skip, int take, bool includeTotal, CancellationToken cancellationToken = default);

    /// <summary>Gets the most containers one page may hold.</summary>
    int MaxTake { get; }
}
