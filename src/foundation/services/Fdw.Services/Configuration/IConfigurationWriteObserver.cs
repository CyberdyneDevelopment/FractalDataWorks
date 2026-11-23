using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Declares that a provider writes more rows, of its own domain or of another, in the unit of work of every write of one of its members (FDW-6121). A provider
/// that does not implement this writes nothing but the member: the declaration is explicit, so no domain's rows are written by accident.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// <para>
/// Why the write asks and not each caller: a copy of a member's data that the callers keep in step drifts the day one caller forgets. The save is where the
/// copy is written, so every caller of <c>Save</c>, <c>Update</c> and <c>Create</c> gets it, whichever overload opened the unit. The description of a data set
/// is the first: it is a column of the data set and a row of the descriptions table, and the two are written in one unit.
/// </para>
/// <para>
/// The removal side already has its seam, <see cref="IReferencedConfigurationProvider{TImplementationConfiguration}"/>, which the delete calls before it
/// retires the member.
/// </para>
/// </remarks>
public interface IConfigurationWriteObserver<in TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Writes, in <paramref name="unit"/>, whatever follows from a member that has just been written.</summary>
    /// <param name="record">The member as written: the whole record, its children included, each with the Id it was saved under.</param>
    /// <param name="unit">The unit the write runs in; what is written here commits or rolls back with it. Every read made here must be a read on the unit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once the rows are written; otherwise the failure that stopped it, and the write does not go ahead: the unit rolls back, so nothing lands half.</returns>
    Task<IGenericResult> Written(TImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);
}
