using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Finds the document storage an organization uses for a purpose (FDW-1581), without the organization naming a connection.
/// </summary>
/// <remarks>
/// The organization's assignment names a grant the tenant holds, and the grant names a connection and a folder. The store returned is
/// narrowed to the grant's folder plus the assignment's base folder. An organization with no assignment for the purpose is refused,
/// never given another organization's store or the whole connection. This is infrastructure for the services that hold documents: it
/// does not judge the caller, which the service using it does.
/// </remarks>
public interface IDocumentStores
{
    /// <summary>Gets the store an organization uses for a purpose.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="purpose">What the store is for (for example Documents).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store, narrowed to the organization's folder, or the reason there is none.</returns>
    Task<IGenericResult<IDocumentStore>> For(Guid organizationId, string purpose, CancellationToken cancellationToken = default);
}
