using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Links;

/// <summary>Reads, adds and removes the links of an organization's documents (FDW-3397). The application implements it over its own storage.</summary>
/// <remarks>An implementation checks every link with <see cref="DocumentLinkRule.Check"/> before it stores it.</remarks>
public interface IDocumentLinks
{
    /// <summary>Lists the links that start from a document.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document's public id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The links, or a failure coded <c>DocumentRecordNotFound</c> when the organization has no such document.</returns>
    Task<IGenericResult<IReadOnlyList<IDocumentLink>>> From(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Lists the links that point at an entity, such as what links to an organization or a record node.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="targetKind">The target kind.</param>
    /// <param name="targetId">The target's public id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The links, an empty list when nothing links to the entity.</returns>
    Task<IGenericResult<IReadOnlyList<IDocumentLink>>> To(Guid organizationId, string targetKind, Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>Adds a link, once <see cref="DocumentLinkRule.Check"/> passes it.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="link">The link.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The organization's new revision, the rule's refusal, or a failure coded <c>DocumentRevisionConflict</c>.</returns>
    Task<IGenericResult<long>> Add(Guid organizationId, IDocumentLink link, long expectedRevision, CancellationToken cancellationToken = default);

    /// <summary>Removes a link.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The link's public id.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The organization's new revision, or a failure coded <c>DocumentRecordNotFound</c> or <c>DocumentRevisionConflict</c>.</returns>
    Task<IGenericResult<long>> Remove(Guid organizationId, Guid id, long expectedRevision, CancellationToken cancellationToken = default);
}
