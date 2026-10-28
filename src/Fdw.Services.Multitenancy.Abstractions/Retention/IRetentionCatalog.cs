using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Tells a client what it can choose when it sets a retention policy or places a legal hold, as the application's
/// <see cref="IRetainedDocuments"/> declares it (FDW-2906).
/// </summary>
/// <remarks>Each method first asks whether the caller holds <c>retention:read</c> for the organization.</remarks>
public interface IRetentionCatalog
{
    /// <summary>Reads the start fields, end actions and document types an organization's documents offer.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The choices; a refusal when the caller may not read the organization's retention.</returns>
    Task<IGenericResult<RetentionChoices>> Choices(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Lists the documents of one type that belong to an organization, with the dates the application knows.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentType">The document type, as named in a retention policy.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The documents; a refusal when the type is blank or the caller may not read the organization's retention.</returns>
    Task<IGenericResult<IReadOnlyList<RetainedDocument>>> Documents(Guid organizationId, string documentType, CancellationToken cancellationToken = default);
}
