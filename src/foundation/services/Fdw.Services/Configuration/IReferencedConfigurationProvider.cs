using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Declares that members of other domains name this provider's members by their durable Id, and that deleting a member removes them in the
/// same unit of work (FDW-5222). A provider that does not implement this removes nothing but its own member: the declaration is explicit, so
/// no domain's rows are removed by accident.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// Why the delete asks and not each caller: a member deleted while another domain still names it leaves that domain holding a reference to
/// nothing, which fails the next read that follows it. A role deleted while people held it left their assignments behind, and each holder's
/// own profile then failed to read. Every caller of the delete gets the removal, because the delete is where it happens.
/// </remarks>
public interface IReferencedConfigurationProvider<in TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Removes, in <paramref name="unit"/>, every row of another domain that names the member about to be deleted.</summary>
    /// <param name="memberId">The durable Id of the member being deleted.</param>
    /// <param name="unit">The unit the delete runs in; the removal commits or rolls back with it.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once every referencing row is removed; otherwise the failure that stopped it, and the delete does not go ahead.</returns>
    Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);
}
