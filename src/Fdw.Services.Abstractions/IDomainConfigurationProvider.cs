using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// The erased surface of a domain's configuration provider: what a caller holding no type argument
/// can ask of it.
/// </summary>
public interface IDomainConfigurationProvider
{
    /// <summary>Gets a configured member's implementation configuration by name.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<IImplementationConfiguration>> Get(string name, CancellationToken ct = default);

    /// <summary>Gets a configured member's implementation configuration by durable id.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<IImplementationConfiguration>> Get(Guid id, CancellationToken ct = default);

    /// <summary>Writes a configured member: its domain row if there is not one, then its implementation.</summary>
    /// <param name="implementationConfiguration">The configuration to write.</param>
    /// <param name="domain">The domain this member belongs to.</param>
    /// <param name="implementationName">Which implementation this is; it selects the provider that writes it.</param>
    /// <param name="name">The member's name, which the domain row carries.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Save(
        IImplementationConfiguration implementationConfiguration,
        string domain,
        string implementationName,
        string name,
        CancellationToken ct = default);

    /// <summary>Deletes a configured member by durable id.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Delete(Guid id, CancellationToken ct = default);

    /// <summary>Deletes a configured member by name.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Delete(string name, CancellationToken ct = default);
}

/// <summary>
/// Supplies the configured members of one domain, resolving each to the implementation its domain
/// row names.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// A domain provider deals with more than one kind of implementation, which is what makes it a
/// domain: it reads the domain row, takes the implementation that row names and the row's own Id,
/// and passes them to the provider registered under that name. It never returns the domain record —
/// the record is how the implementation is found, not the answer.
/// <para>
/// This shares no ancestor with <see cref="IImplementationConfigurationProvider{TConfiguration}"/>.
/// The two read different tables and answer for different things; that their members line up is
/// what a caller sees, not a hierarchy.
/// </para>
/// </remarks>
public interface IDomainConfigurationProvider<TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets a configured member by name.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation configured for that member, or a structured failure.</returns>
    Task<IGenericResult<TImplementationConfiguration>> Get(string name, CancellationToken ct = default);

    /// <summary>Gets a configured member by the domain row's durable id.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation configured for that member, or a structured failure.</returns>
    Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, CancellationToken ct = default);

    /// <summary>Gets a configured member as it stood at an instant.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="asOf">The instant to read as of.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation in force at that instant, or a structured failure.</returns>
    /// <remarks>
    /// The instant reaches the implementation read too. Composing today's implementation onto a
    /// domain row read as of a past instant would answer an as-of question with a mixture of two
    /// versions, and say nothing about having done so.
    /// </remarks>
    Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default);

    /// <summary>Gets every configured member of this domain.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Each member's implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> Get(CancellationToken ct = default);

    /// <summary>Gets the members of this domain whose configuration matches a predicate.</summary>
    /// <typeparam name="T">The implementation to filter on.</typeparam>
    /// <param name="predicate">The test each configuration must satisfy.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The matching configurations, or a structured failure.</returns>
    /// <remarks>
    /// The caller supplies the type. A domain holds only its contract, so a predicate closed over
    /// that contract could test nothing but the name and the domain; naming the implementation is
    /// what lets the predicate reach the properties worth filtering on.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<T>>> Find<T>(Func<T, bool> predicate, CancellationToken ct = default)
        where T : TImplementationConfiguration;

    /// <summary>Writes a configured member: its domain row if there is not one, then its implementation.</summary>
    /// <typeparam name="T">The implementation configuration being written.</typeparam>
    /// <param name="implementationConfiguration">The configuration to write.</param>
    /// <param name="domain">The domain this member belongs to.</param>
    /// <param name="implementationName">Which implementation this is; it selects the provider that writes it.</param>
    /// <param name="name">The member's name, which the domain row carries.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success, or a structured failure.</returns>
    /// <remarks>
    /// One write rather than a create and an update: the name identifies the member, so an existing
    /// domain row carrying it is the row to hang from and only its absence mints a new one.
    /// </remarks>
    Task<IGenericResult> Save<T>(
        T implementationConfiguration,
        string domain,
        string implementationName,
        string name,
        CancellationToken ct = default)
        where T : TImplementationConfiguration;

    /// <summary>Deletes a configured member by durable id.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Delete(Guid id, CancellationToken ct = default);

    /// <summary>Deletes a configured member by name.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Delete(string name, CancellationToken ct = default);

    /// <summary>Registers the provider that supplies one implementation's own configuration.</summary>
    /// <typeparam name="T">The implementation provider being registered.</typeparam>
    /// <param name="name">The value a domain row carries to name this implementation.</param>
    /// <param name="implementationConfigurationProvider">The provider to dispatch to.</param>
    /// <returns>Success, or a failure naming why the provider could not be registered.</returns>
    IGenericResult Register<T>(string name, T implementationConfigurationProvider);
}
