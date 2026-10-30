using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Reads the rows of a data set for a consumer that is not a data query, such as a calculation reading its input. The read goes
/// through the gateway the caller passes, so the data store applies the access of whoever that gateway belongs to.
/// </summary>
public interface IDataSetRowReader
{
    /// <summary>Reads every row of a data set, refusing a data set that holds more than the cap.</summary>
    /// <param name="gateway">The data gateway to read through; the rows are the ones its holder may read.</param>
    /// <param name="dataSetName">The name of the data set.</param>
    /// <param name="rowCap">The most rows to read; a data set with more is refused, never cut short.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The rows, each by column name; a failure when the data set is absent, is a view that carries a stored filter, cannot be
    /// read in the store, or holds more than <paramref name="rowCap"/> rows.
    /// </returns>
    Task<IGenericResult<IReadOnlyList<Dictionary<string, object?>>>> Read(
        IDataGateway gateway,
        string dataSetName,
        int rowCap,
        CancellationToken cancellationToken = default);
}
