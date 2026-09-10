using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// The erased surface of a configuration provider: what a caller holding no type argument can ask.
/// </summary>
public interface IImplementationConfigurationProvider
{
    /// <summary>Gets a configured member's implementation configuration, resolved through its domain record.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<IImplementationConfiguration>> Get(string name, CancellationToken ct = default);

    /// <summary>Gets a configured member's implementation configuration by the domain record's durable id.</summary>
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
    /// <remarks>
    /// One write rather than a create and an update: the name identifies the member, so an existing
    /// domain row carrying it is the row to hang from and only its absence mints a new one.
    /// </remarks>
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
/// Supplies and persists one implementation's configuration, keyed by the domain row that owns it.
/// </summary>
/// <typeparam name="TConfiguration">The domain's implementation configuration contract.</typeparam>
/// <remarks>
/// <c>Get(Guid)</c> takes the owning domain row's durable <c>Id</c> and resolves the implementation row
/// through the foreign key discovered from the data-store tree — the RowId match happens inside the
/// join, so no RowId is ever materialised in C#.
/// <para>
/// Close this over the domain's implementation <i>contract</i>, never over a single implementation's
/// concrete class, so one constraint accepts every provider a domain has.
/// </para>
/// </remarks>
public interface IImplementationConfigurationProvider<TConfiguration>
    where TConfiguration : IImplementationConfiguration
{
    /// <summary>Gets the implementation configuration owned by a domain row.</summary>
    /// <param name="domainId">The owning domain row's durable id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TConfiguration>> Get(Guid domainId, CancellationToken cancellationToken = default);

    /// <summary>Reads this implementation's row as it stood at an instant.</summary>
    /// <param name="domainId">The domain record's durable id.</param>
    /// <param name="asOf">The instant to read as of.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation in force at that instant, or a structured failure.</returns>
    /// <remarks>
    /// Required, not optional: composing today's implementation onto a domain row read as of a past
    /// instant would answer an as-of question with a mixture of two versions, and say nothing about
    /// having done so.
    /// </remarks>
    Task<IGenericResult<TConfiguration>> Get(Guid domainId, DateTimeOffset asOf, CancellationToken cancellationToken = default);

    /// <summary>Gets the implementation configurations owned by several domain rows.</summary>
    /// <param name="domainIds">The owning domain rows' durable ids.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configurations, or a structured failure.</returns>
    Task<IGenericResult<IReadOnlyList<TConfiguration>>> Get(IEnumerable<Guid> domainIds, CancellationToken cancellationToken = default);

    /// <summary>Gets the implementation configurations matching a predicate.</summary>
    /// <param name="predicate">The test each configuration must satisfy.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching configurations, or a structured failure.</returns>
    Task<IGenericResult<IReadOnlyList<TConfiguration>>> Find(Func<TConfiguration, bool> predicate, CancellationToken cancellationToken = default);

    /// <summary>Gets every implementation configuration this provider owns.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configurations, or a structured failure.</returns>
    Task<IGenericResult<IReadOnlyList<TConfiguration>>> Get(CancellationToken cancellationToken = default);

    /// <summary>Saves an implementation configuration.</summary>
    /// <param name="record">The configuration to save.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The saved configuration, or a structured failure.</returns>
    Task<IGenericResult<TConfiguration>> Save(TConfiguration record, CancellationToken cancellationToken = default);

    /// <summary>Deletes the implementation configuration owned by a domain row.</summary>
    /// <param name="domainId">The owning domain row's durable id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Delete(Guid domainId, CancellationToken cancellationToken = default);
}
