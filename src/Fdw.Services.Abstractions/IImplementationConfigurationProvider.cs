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
/// Close this over the domain's implementation <i>contract</i> — <c>IConnectionImplementationConfiguration</c> — never
/// over a single implementation's concrete class. That is what lets
/// <c>Register&lt;T&gt;(name, provider) where T : IImplementationConfigurationProvider&lt;TConfiguration&gt;</c>
/// accept every one of a domain's providers without erasing the type.
/// <para>
/// Variance cannot do that job here, so the typing has to. <c>Save</c> puts the configuration in an
/// input position, and <c>Task&lt;&gt;</c> is a class and therefore invariant in its own argument, so
/// <c>out</c> is rejected even though <c>IGenericResult&lt;out T&gt;</c> is itself covariant.
/// </para>
/// <para>
/// Lookup is by the owning domain row's <c>RowId</c>, which is the column the foreign key is declared
/// on. The domain configuration provider holds these keyed by <c>ServiceOptionType</c> and calls the
/// one the domain row names.
/// </para>
/// </remarks>
public interface IImplementationConfigurationProvider<TConfiguration>
    where TConfiguration : IGenericConfiguration
{
    /// <summary>Gets the implementation configuration owned by a domain row.</summary>
    /// <param name="domainRowId">The owning domain row's physical identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configuration, or a structured failure.</returns>
    Task<IGenericResult<TConfiguration>> Get(int domainRowId, CancellationToken cancellationToken = default);

    /// <summary>Gets every implementation configuration this provider owns.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The implementation configurations, or a structured failure.</returns>
    Task<IGenericResult<IReadOnlyList<TConfiguration>>> Get(CancellationToken cancellationToken = default);

    /// <summary>Writes both halves of a configured member — the domain row and its implementation row.</summary>
    /// <param name="configuration">The domain configuration holding the implementation to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The saved implementation configuration, or a structured failure.</returns>
    /// <remarks>
    /// One provider owns the whole write. For a new member it inserts the domain row first, takes the
    /// <c>RowId</c> the database assigned, stamps it on the implementation row and inserts that. The
    /// order is forced: the implementation's foreign key is declared on the domain's <c>RowId</c>, so
    /// the domain row has to exist before the implementation row can name it.
    /// <para>
    /// This is why <c>Save</c> takes the domain configuration rather than the implementation alone — the
    /// implementation carries only its owner's id, never the owner's <c>Name</c>,
    /// <c>ServiceOptionType</c> or <c>Description</c>, so it cannot describe the row it belongs to.
    /// </para>
    /// </remarks>
    Task<IGenericResult<TConfiguration>> Save(
        IPlatformServiceConfiguration<TConfiguration> configuration,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes both halves of a configured member.</summary>
    /// <param name="domainRowId">The owning domain row's physical identity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or a structured failure.</returns>
    /// <remarks>
    /// The implementation row goes first and the domain row second, which is the insert order reversed
    /// — the same foreign key that forced one forces the other.
    /// </remarks>
    Task<IGenericResult> Delete(int domainRowId, CancellationToken cancellationToken = default);
}
