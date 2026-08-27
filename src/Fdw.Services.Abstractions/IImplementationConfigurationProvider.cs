using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

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
