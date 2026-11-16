using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions.ColumnValues;

/// <summary>
/// Searches the distinct values one column of a data store container holds (FDW-5666), so a person can pick a few among many.
/// </summary>
/// <remarks>
/// The store does the work: the distinct values with a row count each, the contains match and the paging are one grouped query
/// sent through the data gateway, so a column is never read whole into the caller.
/// </remarks>
public interface IContainerColumnValues
{
    /// <summary>Answers one page of the distinct values of a container column that contain the search text.</summary>
    /// <param name="query">The container, the column, the search text, the page and the values already chosen.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page, or the coded failure that says why the values could not be read.</returns>
    Task<IGenericResult<ColumnValuesPage>> Search(ColumnValuesQuery query, CancellationToken cancellationToken = default);
}
