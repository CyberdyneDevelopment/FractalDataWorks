using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Decides whether the caller owns a variation, and so may be told what it varies: its parent, its root's id and its filters (FDW-5446).
/// </summary>
/// <remarks>
/// A person who holds only a variation sees one data set and nothing of the chain behind it. The owner of a Scoped variation is the workspace that owns
/// it, as a caller acting in it; the owner of a data view is the user who owns it. The data package registers an answer for a data view and refuses a
/// Scoped one; a package that knows workspaces registers one that answers for both, in its place.
/// </remarks>
public interface IVariationChainAccess
{
    /// <summary>Asks whether the caller owns the variation.</summary>
    /// <param name="variation">The variation whose chain is asked about.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True when the caller owns it; false when they hold it without owning it; a failure when ownership could not be decided.</returns>
    Task<IGenericResult<bool>> MayShowChain(IDataSetVariationImplementationConfiguration variation, CancellationToken cancellationToken = default);
}
