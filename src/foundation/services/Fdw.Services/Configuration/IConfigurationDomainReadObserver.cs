using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Declares that a provider fills members of the domain records it answers a list or a read by Ids with, from rows of another domain, once for the whole page
/// (FDW-6208). The domain record is answered from the domain table or a view alone, so a text that depends on who asks (a description is in the language of the
/// caller's tenant) is not a column of it and cannot be joined in: this fills it.
/// </summary>
/// <typeparam name="TDomain">The domain record the provider answers with.</typeparam>
/// <remarks>
/// The twin of <see cref="IConfigurationReadObserver{TImplementationConfiguration}"/>, for the domain records of <c>Get(DomainQuery)</c> and <c>Get(ids)</c>
/// where that one is for the composed implementation records. It is tested with <c>this is ...</c>, so a provider that does not declare it answers what its rows
/// hold. The descriptions domain and the domains it reads to answer (settings, authentication) never declare it.
/// </remarks>
public interface IConfigurationDomainReadObserver<in TDomain>
{
    /// <summary>Fills, in place, what the domain records read from the provider's own rows do not hold.</summary>
    /// <param name="records">The records of the page just read, in the order read; never empty.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once every record is filled; otherwise the failure that stopped it, and the read answers it.</returns>
    Task<IGenericResult> Read(IReadOnlyList<TDomain> records, CancellationToken cancellationToken = default);
}
