using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Runs the retention policies of an organization against the application's documents (FDW-1575).
/// </summary>
/// <remarks>
/// For each document type with a policy the keeper asks the application's <see cref="IRetainedDocuments"/> for the documents, works out
/// each deadline with <see cref="RetentionClock"/>, and asks the application to end the ones past it, except the ones under a legal hold.
/// A document that cannot be ended is reported and the run goes on with the rest.
/// </remarks>
public interface IRetentionKeeper
{
    /// <summary>Runs the policies of an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="now">The moment every deadline is judged against.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>What the run found and did, or a failure when the policies, the holds or a document listing could not be read.</returns>
    Task<IGenericResult<RetentionRunReport>> Run(Guid organizationId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
