using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The contract a document application implements so the retention job can work on its documents (FDW-1575).
/// </summary>
/// <remarks>
/// The platform owns the policy, the hold, the date rule, the job and the audit. It does not own anyone's document records, so the
/// application lists them and ends them: nothing about its document store is assumed.
/// </remarks>
public interface IRetainedDocuments
{
    /// <summary>Lists the documents of one type that belong to an organization.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentType">The document type, as named in a retention policy.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The documents with the dates they have.</returns>
    Task<IGenericResult<IReadOnlyList<RetainedDocument>>> List(Guid tenantId, Guid organizationId, string documentType, CancellationToken cancellationToken = default);

    /// <summary>Ends a document whose period is over.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="document">The document to end.</param>
    /// <param name="endAction">The end action named in the document type's retention policy (for example Delete or Archive); an action the application does not know is a failure.</param>
    /// <param name="cancellationToken">Cancels the action.</param>
    /// <returns>Success once the document is ended.</returns>
    Task<IGenericResult> End(Guid tenantId, Guid organizationId, RetainedDocument document, string endAction, CancellationToken cancellationToken = default);
}
