using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// The one place an administrative change is recorded and read back. Every administrative action calls <see cref="Record"/>;
/// none writes its own row.
/// </summary>
/// <remarks>
/// A change that cannot be recorded fails the action that made it: <see cref="Record"/> answers a failure and the caller
/// returns it, so an administrative change never takes effect unaudited. The record is append only; there is no update and no
/// delete.
/// </remarks>
public interface IAdministrativeAudit
{
    /// <summary>
    /// Records a change the caller has made. The actor and the tenant are the caller's own, from its authentication context.
    /// </summary>
    /// <param name="entry">What was changed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the change was recorded; otherwise the reason it was not.</returns>
    Task<IGenericResult> Record(AdministrativeChangeEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads recorded changes, newest first, within the caller's tenant scope.
    /// </summary>
    /// <param name="filter">Which changes, and which page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of records, or the reason they could not be read.</returns>
    Task<IGenericResult<IPagedResponse<AdministrativeChangeRecord>>> Read(AdministrativeChangeFilter filter, CancellationToken cancellationToken = default);
}
