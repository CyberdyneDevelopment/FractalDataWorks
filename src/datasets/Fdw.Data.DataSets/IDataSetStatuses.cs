using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The status of the data sets the caller may see (FDW-4488).
/// </summary>
/// <remarks>
/// <para>
/// The data set domain owns it, and a list or a console asks it by this interface rather than reading the writers, the runs, the settings and the clock itself. It is scoped,
/// because the tenant, the writers and the runs are.
/// </para>
/// <para>
/// A status is the highest-ranked option of <c>DataSetStatuses</c> whose rule applies. The window it is judged by is the setting <c>DataSetStatusWindowHours</c> as it resolves for
/// the caller's tenant; nothing assumes one.
/// </para>
/// </remarks>
public interface IDataSetStatuses
{
    /// <summary>Reads what a status is judged by, once, for a request that works out the status of several data sets.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The view, or the failure that names what could not be read.</returns>
    Task<IGenericResult<IDataSetStatusView>> Read(CancellationToken cancellationToken = default);

    /// <summary>Counts the data sets the caller may see by status.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Every status of <c>DataSetStatuses</c>, highest rank first, with the number of data sets that have it, so a status nothing has reads 0. Variations are not counted. Or the
    /// failure that names what could not be read.
    /// </returns>
    Task<IGenericResult<IReadOnlyList<DataSetStatusCount>>> Counts(CancellationToken cancellationToken = default);
}
