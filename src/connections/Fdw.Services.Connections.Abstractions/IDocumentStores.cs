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
/// never given another organization's store or the whole connection. A file keeps resolving through the assignment it was stored under
/// after the organization is assigned elsewhere (FDW-3396). This is infrastructure for the services that hold documents: it
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

    /// <summary>
    /// Gets the store a file was written through, by the assignment it was stored under (FDW-3396), whether that assignment is still in
    /// force or was superseded since.
    /// </summary>
    /// <remarks>
    /// Read files through this, and write new ones through <see cref="For"/>. An assignment that does not exist is refused, never
    /// replaced by the organization's current store; an assignment of another organization is refused.
    /// </remarks>
    /// <param name="organizationId">The organization reading the file.</param>
    /// <param name="storedUnder">The <see cref="IDocumentStore.StoredUnder"/> of the store the file was written through.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store as it was when the file was written, narrowed to the same folder, or the reason there is none.</returns>
    Task<IGenericResult<IDocumentStore>> Stored(Guid organizationId, Guid storedUnder, CancellationToken cancellationToken = default);
}
