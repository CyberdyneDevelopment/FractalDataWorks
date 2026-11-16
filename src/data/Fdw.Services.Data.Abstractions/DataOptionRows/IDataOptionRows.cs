using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>
/// Lists, adds, changes and removes the option rows of a data store container (FDW-5664), in the one options table the server
/// settings <c>DataOptionsDataStore</c>, <c>DataOptionsPath</c> and <c>DataOptionsContainer</c> name.
/// </summary>
/// <remarks>
/// Every read and write goes through the data gateway, set-based: a list is one paged read, an add one multi-row insert after one
/// read of the pairs already stored, a remove one read and one delete. A refused request writes nothing.
/// </remarks>
public interface IDataOptionRows
{
    /// <summary>Reads one page of a container's option rows, in column then value order.</summary>
    /// <param name="query">The container, the column if one, and the page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page, or the coded failure that says why it could not be read.</returns>
    Task<IGenericResult<DataOptionRowsPage>> List(DataOptionRowsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Reads one option row of a container by its id.</summary>
    /// <param name="key">The container and the row's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The row; a success holding null when the container has no row with that id; or the failure of the read.</returns>
    Task<IGenericResult<DataOptionRow?>> Get(DataOptionRowKey key, CancellationToken cancellationToken = default);

    /// <summary>Adds several option rows to a container at once: all of them, or none when any is refused.</summary>
    /// <param name="addition">The container and the rows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows as stored, each with its id; or the coded refusal naming the row and why.</returns>
    Task<IGenericResult<IReadOnlyList<DataOptionRow>>> Add(DataOptionRowsAddition addition, CancellationToken cancellationToken = default);

    /// <summary>Changes the option, whether it is active, or the value of one row; only the fields the change states.</summary>
    /// <param name="change">The container, the row's id and what changes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The row as it is stored afterwards, or the coded refusal.</returns>
    Task<IGenericResult<DataOptionRow>> Change(DataOptionRowChange change, CancellationToken cancellationToken = default);

    /// <summary>Removes several option rows of a container by id: all of them, or none when any id is not the container's.</summary>
    /// <param name="removal">The container and the ids.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>How many rows were removed, or the coded refusal naming the ids that are not the container's.</returns>
    Task<IGenericResult<int>> Remove(DataOptionRowsRemoval removal, CancellationToken cancellationToken = default);
}
