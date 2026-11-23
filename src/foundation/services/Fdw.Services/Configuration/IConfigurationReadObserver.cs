using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Declares that a provider fills members of what it reads from rows of another domain, once for the whole set it has just read (FDW-6208). A provider that
/// does not implement this answers exactly what its own rows hold: the declaration is explicit, so no read is changed by accident.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// <para>
/// Why a set and not a record: a page of 580 data sets must cost one read of the descriptions, never 580. Every read path calls this once, after the
/// records are composed and their identity is stamped (the Id of a record is the domain Id until then not the one other domains key their rows by): a read of
/// one member by id, name or narrowing calls it with a list of one, and the page, the flat and the whole read call it with the whole set.
/// </para>
/// <para>
/// A read as of a past instant is refused for a provider that declares this, with a structured failure, because what it fills has no as-of read: today's
/// text on a past version would be a wrong answer that looks right.
/// </para>
/// <para>
/// The descriptions domain, and every domain it reads to answer (settings, authentication), never implement this: a fill that reads a description would
/// otherwise start a fill of its own. The write side is <see cref="IConfigurationWriteObserver{TImplementationConfiguration}"/>.
/// </para>
/// </remarks>
public interface IConfigurationReadObserver<in TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Fills, in place, what the records read from this provider's own rows do not hold.</summary>
    /// <param name="records">The records just composed, in the order read; never empty.</param>
    /// <param name="asOf">The instant asked for, which a provider of this kind refuses before it is called, so it is always <see langword="null"/> here.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once every record is filled; otherwise the failure that stopped it, and the read answers it.</returns>
    Task<IGenericResult> Read(IReadOnlyList<TImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default);
}
