using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Reads a domain's list from its domain table alone.
/// </summary>
/// <typeparam name="TDomain">The domain's record: the four properties every domain has, plus the fields its list shows.</typeparam>
/// <remarks>
/// A list of members needs a few fields of each, not the member. Those fields live on the domain row,
/// so one query answers a page: the filter, the ordering and the paging are all done by the store,
/// and no implementation provider is consulted. Reading a member whole is still
/// <see cref="IDomainConfigurationProvider{TImplementationConfiguration}"/>.
/// </remarks>
public interface IDomainRecordProvider<TDomain>
    where TDomain : class, IDomainConfiguration
{
    /// <summary>Gets one page of this domain's members, as domain records, in one query.</summary>
    /// <param name="query">Which members, in what order, which page, and whether a total is wanted.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// The page, or a structured failure when the query is not valid (a negative skip, a take below
    /// one) or the store could not answer it. The page carries a total only when the query asked for one.
    /// </returns>
    Task<IGenericResult<ConfigurationPage<TDomain>>> Get(DomainQuery query, CancellationToken ct = default);

    /// <summary>Gets the members with the given ids, as domain records, in one query.</summary>
    /// <param name="ids">The ids wanted. Duplicates are asked once; none asked answers none without a query.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// The members found, in no promised order, or a structured failure when more ids are named than one
    /// query may carry or the store could not answer. A member the caller may not see under row-level
    /// security is not in the answer: absent, not an error.
    /// </returns>
    /// <remarks>
    /// This is how one domain names another's members without joining across domains: a data store's page
    /// asks the connection domain's provider for the connections its stores use, and gets their names from
    /// the connection domain table alone.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<TDomain>>> Get(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}
