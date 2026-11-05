using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// Finds an organization's document records (FDW-3549, FDW-3673), as <c>IDocumentStores.For</c> finds where its files are.
/// </summary>
/// <remarks>
/// The organization's DocumentRecord connection comes from its assignment for the purpose <c>DocumentRecords</c>, which names a grant its
/// tenant holds (FDW-1581). No assignment, a grant that is gone, or a connection that is not a DocumentRecord connection with the capabilities
/// the store needs is refused, naming the organization, never answered with another connection.
/// </remarks>
public interface IDocumentRecordStores
{
    /// <summary>Gets an organization's document records.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The organization's records, or the reason there are none.</returns>
    Task<IGenericResult<IDocumentRecordStore>> For(Guid organizationId, CancellationToken cancellationToken = default);
}
