using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Declares that a provider needs something from another store to write a member, and that it must have it BEFORE the unit of the write opens (FDW-7256).
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// <para>
/// Why before: a read made on another connection while the unit holds locks on the rows the write has touched waits for the unit to commit, and the unit is waiting for that
/// read: the save never returns. Every store here runs under READ COMMITTED with no snapshot, so a reader blocks on a writer's uncommitted rows. The first case is a server
/// setting saved with a description: the description is written in the language the settings state, and the settings are read from the very tables the save is writing.
/// </para>
/// <para>
/// The base calls <see cref="Prepare"/> once for each write that opens its own unit (<c>Save</c>, <c>Update</c> and <c>Create</c> without a unit), with the record that
/// <see cref="IConfigurationWriteObserver{TImplementationConfiguration}.Written"/> is later given: the same instance, so the provider may key what it resolved by it. A write made
/// into a unit the caller opened is not prepared, because the unit is already open; a provider that needs its preparation for that write refuses it, loudly.
/// </para>
/// </remarks>
public interface IConfigurationWritePreparer<in TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Resolves what writing <paramref name="record"/> needs from other stores, before any unit is open.</summary>
    /// <param name="record">The member about to be written.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once everything is resolved; otherwise the failure that stops the write: no unit is opened and nothing is written.</returns>
    Task<IGenericResult> Prepare(TImplementationConfiguration record, CancellationToken cancellationToken = default);
}
