using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions;

/// <summary>
/// Generic client interface for querying resources by list and by name.
/// Domain-specific clients implement this alongside their domain interface to provide
/// a composable, uniform query surface across all resource types.
/// </summary>
/// <typeparam name="TSummary">The summary DTO type returned in list operations.</typeparam>
/// <typeparam name="TDetail">The detail DTO type returned in get-by-name operations.</typeparam>
public interface IResourceQueryClient<TSummary, TDetail>
{
    /// <summary>
    /// Lists all resources of this type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the list of resource summaries.</returns>
    Task<IGenericResult<IReadOnlyList<TSummary>>> List(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single resource by name.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the resource detail.</returns>
    Task<IGenericResult<TDetail>> Get(string name, CancellationToken cancellationToken = default);
}
