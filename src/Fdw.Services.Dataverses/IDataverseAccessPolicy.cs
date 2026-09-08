using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Dataverses;

/// <summary>
/// Decides whether the calling user may change a particular dataverse.
/// </summary>
/// <remarks>
/// <c>dataverses:write</c> answers "may this caller have dataverses at all". It does not answer
/// "which ones", and until this existed nothing did: the row-level policy filters on tenant and
/// visibility group, the endpoints never read <c>OwnerUserId</c> or <c>DataverseMember</c>, and the
/// four member roles were a vocabulary no authorization path consulted. Anyone holding the
/// permission could edit or delete every dataverse in the tenant.
/// <para>
/// One decision, in one place, because the next dataverse-scoped write — a note, a resource, a
/// relationship — asks the same question, and a rule re-derived per endpoint is a rule that will
/// eventually differ per endpoint.
/// </para>
/// </remarks>
public interface IDataverseAccessPolicy
{
    /// <summary>Whether the calling user may change <paramref name="dataverse"/>.</summary>
    /// <param name="dataverse">The dataverse, with its members loaded.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Success when the caller may write it; a failure carrying the reason when they may not.
    /// A result rather than a bool, so a refusal reaches the caller as the refusal it is instead of
    /// as a value somebody has to remember to turn back into one.
    /// </returns>
    Task<IGenericResult> MayWrite(DataverseConfiguration dataverse, CancellationToken cancellationToken = default);
}
