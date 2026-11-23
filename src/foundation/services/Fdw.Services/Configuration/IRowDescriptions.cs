using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Where the description of the rows of a container is stored when it is not a column of the container's own table (FDW-6208): the search of such a
/// container asks it which rows a text finds, and the text of the rows it returns.
/// </summary>
/// <remarks>
/// The rows are named by their logical Id, the key the descriptions table keeps for the thing described. An answer is whole: a search that names the rows a
/// text finds in chunks of the gateway's key chunk size loses none, and no list is cut or refused.
/// </remarks>
public interface IRowDescriptions
{
    /// <summary>Finds the rows whose description contains a text.</summary>
    /// <param name="text">The text to find.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The logical Ids of the rows described by a text that contains it, or the failure of the read.</returns>
    Task<IGenericResult<IReadOnlyCollection<Guid>>> RowsDescribedBy(string text, CancellationToken cancellationToken = default);

    /// <summary>Reads the description of some rows.</summary>
    /// <param name="ids">The logical Ids of the rows.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The text of each row that has a description, by the Id of the row, or the failure of the read.</returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> TextsOf(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
