using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Declares that a provider fills what the children of a child read do not hold, from rows of another domain, once for the read (FDW-6777). A provider that does
/// not implement this answers its children exactly as their own rows hold them: the declaration is explicit, so no read is changed by accident.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// <para>
/// Why a third observer: <see cref="IConfigurationReadObserver{TImplementationConfiguration}"/> is handed the members a read composed, and the domain-record page
/// has <see cref="IConfigurationDomainReadObserver{TDomain}"/>. The two child reads, <c>Get&lt;TChild&gt;</c> and <c>Get&lt;TOwner, TChild&gt;</c>, compose no
/// member: they read one member's children straight from the implementation provider, which is how the data set field list reads its fields. Without this
/// their children are never filled, and a moved description answers empty.
/// </para>
/// <para>
/// The member's Id is handed with the children because a child's description is owned by the member it hangs from (a data set's fields; a data store's paths,
/// containers, fields and keys), and the children are the read's own list, filled in place. It is called once for each read that found children, never with an
/// empty list. The descriptions domain, and every domain it reads to answer (settings, authentication), never implement this, as for the read observer.
/// </para>
/// </remarks>
public interface IConfigurationChildReadObserver<in TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Fills, in place, what the children of one member's child read do not hold.</summary>
    /// <typeparam name="TChild">The type of the children read.</typeparam>
    /// <param name="memberId">The domain Id of the member the children hang from.</param>
    /// <param name="children">The children read, in the order read; never empty. A child of a type the provider fills nothing on is left as read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once every child is filled; otherwise the failure that stopped it, and the read answers it.</returns>
    Task<IGenericResult> ReadChildren<TChild>(Guid memberId, IReadOnlyList<TChild> children, CancellationToken cancellationToken = default);
}
