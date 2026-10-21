using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The legal holds of an organization's documents (FDW-1575).
/// </summary>
/// <remarks>
/// Who may place or lift a hold is settled by the caller (the <c>legal-holds:write</c> right); a hold is placed on a document id the
/// application chose and the platform never reads the document.
/// </remarks>
public interface ILegalHolds
{
    /// <summary>Reads the holds of an organization.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="heldOnly">True to read only the holds that stand, false to read the lifted ones as well.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The holds, newest first.</returns>
    Task<IGenericResult<IReadOnlyList<LegalHold>>> List(Guid tenantId, Guid organizationId, bool heldOnly, CancellationToken cancellationToken = default);

    /// <summary>Places a hold on a document.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The application's identifier of the document.</param>
    /// <param name="reason">Why the document is held.</param>
    /// <param name="placedBy">The user placing the hold.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The hold, or a conflict failure when the document is already held.</returns>
    Task<IGenericResult<LegalHold>> Place(
        Guid tenantId, Guid organizationId, string documentId, string reason, string placedBy, CancellationToken cancellationToken = default);

    /// <summary>Lifts the hold on a document.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The application's identifier of the document.</param>
    /// <param name="liftedBy">The user lifting the hold.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The hold as lifted, or a not-found failure when the document is not held.</returns>
    Task<IGenericResult<LegalHold>> Lift(
        Guid tenantId, Guid organizationId, string documentId, string liftedBy, CancellationToken cancellationToken = default);

    /// <summary>Reads the ids of the documents of an organization that are held now.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The held document ids, compared ordinally.</returns>
    Task<IGenericResult<ISet<string>>> HeldDocuments(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);
}
