using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Resolves a domain's configured members and routes each to the implementation provider that owns it.
/// </summary>
/// <typeparam name="TConfiguration">The domain's implementation configuration contract.</typeparam>
/// <remarks>
/// It queries its <c>IConfigurationGateway</c> for the domain's configurations, finds the member
/// by name or id, reads the <c>ServiceOptionType</c> that member names, and passes the member's
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
public interface IDomainConfigurationProvider<TConfiguration>
    where TConfiguration : IImplementationConfiguration
{
    /// <summary>Gets a configured member's implementation configuration by name.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TConfiguration>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a configured member's implementation configuration by durable id.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TConfiguration>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Writes a configured member — its domain row and its implementation row.</summary>
    /// <typeparam name="T">The implementation configuration being written.</typeparam>
    /// <param name="serviceOptionType">Which implementation this member is.</param>
    /// <param name="name">The member's name.</param>
    /// <param name="implementationConfiguration">The implementation's own configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Save<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration,
        CancellationToken cancellationToken = default)
        where T : TConfiguration;

    /// <summary>Deletes a configured member by durable id.</summary>
    /// <param name="id">The member's durable id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deletes a configured member by name.</summary>
    /// <param name="name">The member's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    Task<IGenericResult> Delete(string name, CancellationToken cancellationToken = default);

    /// <summary>Registers the implementation configuration provider for one ServiceOptionType.</summary>
    /// <typeparam name="T">The implementation provider being registered.</typeparam>
    /// <param name="name">The ServiceOptionType this provider owns.</param>
    /// <param name="implementationConfigurationProvider">The provider.</param>
    /// <returns>Success, or a structured failure.</returns>
    IGenericResult Register<T>(string name, T implementationConfigurationProvider)
        where T : IImplementationConfigurationProvider<TConfiguration>;
}
