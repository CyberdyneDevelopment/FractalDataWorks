using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>
/// Reads the definitions of one kind that bear on an organization (FDW-3398): the platform's, its tenant's and its own. The application
/// implements it over its own storage, one for each kind of definition; <see cref="ScopedDefinitions"/> decides which one is in force.
/// </summary>
/// <typeparam name="T">The kind of definition.</typeparam>
public interface IDocumentDefinitions<T>
    where T : IScopedDefinition
{
    /// <summary>Lists the platform's definitions of the kind, the tenant's and, when an organization is named, the organization's.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the tenant's own view.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rows of the three levels, as they are stored; an empty list when none of them defines anything of the kind.</returns>
    Task<IGenericResult<IReadOnlyList<T>>> Rows(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default);
}
