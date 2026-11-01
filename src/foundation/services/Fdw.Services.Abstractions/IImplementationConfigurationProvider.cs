using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Supplies one implementation's configuration, keyed by the domain row that owns it.
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
    /// <summary>Gets the container this implementation's rows live in, as its registration named it.</summary>
    /// <remarks>
    /// The domain provider writes this implementation's row under its own: it fills the Foreign key
    /// this container declares to the domain container from the domain row's key values.
    /// </remarks>
    string ContainerName { get; }

    /// <summary>Gets the implementation configuration owned by a domain row.</summary>
    /// <param name="domainId">The owning domain row's durable id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TConfiguration>> Get(Guid domainId, CancellationToken cancellationToken = default);

    /// <summary>Gets the implementation configuration owned by a domain row, composed to a depth.</summary>
    /// <param name="domainId">The owning domain row's durable id.</param>
    /// <param name="levels">
    /// How many levels of child collections to read under the row; see <see cref="ConfigurationLevels"/>.
    /// Collections below that depth are left empty, which means not read.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TConfiguration>> Get(Guid domainId, int levels, CancellationToken cancellationToken = default);

    /// <summary>Gets one child collection of one owner row, without reading the rest of the aggregate.</summary>
    /// <typeparam name="TOwner">
    /// The owner's type: this implementation's contract or configuration for its direct children, or a
    /// child configuration type (a path, a container) for the collections under it.
    /// </typeparam>
    /// <typeparam name="TChild">The type of the collection's items, as the owner declares the collection.</typeparam>
    /// <param name="ownerId">
    /// The owner's durable id: the domain row's id when the owner is this implementation, otherwise the
    /// owner row's own Logical key.
    /// </param>
    /// <param name="levels">How many levels of child collections to read under each item.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The owner's current items in that collection, or a structured failure.</returns>
    /// <remarks>
    /// The owner is identified by its id alone. The table and the key to read through come from the
    /// owner type's declared collection of <typeparamref name="TChild"/> and the DataStore configuration,
    /// exactly as a whole-aggregate read finds them.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<TChild>>> Get<TOwner, TChild>(Guid ownerId, int levels, CancellationToken cancellationToken = default);

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
}
