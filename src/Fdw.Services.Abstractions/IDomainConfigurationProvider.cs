using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Resolves a domain's configured members and routes each to the implementation provider that owns it.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation configuration contract.</typeparam>
/// <remarks>
/// It queries its <c>IConfigurationGateway</c> for the domain's configurations, finds the member
/// by name or id, reads the <c>Implementation</c> that member names, and passes the member's
/// durable <c>Id</c> to the implementation provider registered under that type. What comes back is
/// the implementation configuration, ready for a factory.
/// <para>
/// The <c>Id</c> and not the <c>RowId</c>: <see cref="IImplementationConfigurationProvider{T}.Get(Guid, CancellationToken)"/>
/// takes a <c>Guid</c>, and resolves the implementation row through the foreign key discovered from
/// the data-store tree. The <c>RowId</c> match happens inside that join, so no <c>RowId</c> is ever
/// materialised in C#.
/// </para>
/// <para>
/// It is the only thing holding a gateway, and the only thing knowing which connection the domain lives
/// in. Implementation providers receive the gateway as an argument, so they cannot read from a different
/// store than their domain — which the foreign key already required, since it is declared on the domain
/// row's <c>RowId</c> and cannot span connections.
/// </para>
/// </remarks>
public interface IDomainConfigurationProvider<TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets a configured member's implementation configuration, resolved through its domain record.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TImplementationConfiguration>> Get(string name, CancellationToken ct = default);

    /// <summary>Gets a configured member's implementation configuration by the domain record's durable id.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, CancellationToken ct = default);

    /// <summary>Lists the domain's records.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Every domain record, or a structured failure.</returns>
    /// <remarks>
    /// The domain records themselves, not their implementations: a list is what a caller browses to
    /// find the member it wants, and each record names its own implementation.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<IDomainConfiguration>>> Get(CancellationToken ct = default);

    /// <summary>Updates an existing member's implementation configuration.</summary>
    /// <typeparam name="T">The implementation configuration being written.</typeparam>
    /// <param name="implementationConfiguration">The configuration to write.</param>
    /// <param name="domainId">The domain record this implementation hangs from.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    /// <remarks>
    /// The domain row already exists, so it states the name and the implementation -- there is
    /// nothing for the caller to restate and nothing to create.
    /// </remarks>
    Task<IGenericResult> Save<T>(
        T implementationConfiguration,
        Guid domainId,
        CancellationToken ct = default)
        where T : TImplementationConfiguration;

    /// <summary>Writes a new configured member: its domain record, then its implementation.</summary>
    /// <typeparam name="T">The implementation configuration being written.</typeparam>
    /// <param name="implementationConfiguration">The configuration to write.</param>
    /// <param name="implementationName">Which implementation this is -- it selects the provider that writes it.</param>
    /// <param name="name">The member's name, which the new domain row carries.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    /// <remarks>
    /// There is no domain record yet, so this creates one and writes the implementation against the
    /// id it was assigned.
    /// </remarks>
    Task<IGenericResult> Save<T>(
        T implementationConfiguration,
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

    /// <summary>Registers the implementation configuration provider for one Implementation.</summary>
    /// <typeparam name="T">The implementation provider being registered.</typeparam>
    /// <param name="name">The Implementation this provider owns.</param>
    /// <param name="implementationConfigurationProvider">The provider.</param>
    /// <returns>Success, or a structured failure.</returns>
    IGenericResult Register<T>(string name, T implementationConfigurationProvider);
}
