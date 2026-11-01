using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The one question for a thing that sits in the scope tree: may the caller do this to it, judged from the rights they hold at its scope
/// or above (FDW-2326, FDW-2327).
/// </summary>
/// <remarks>
/// This is a rule type, not a policy name. The framework gives the question and a default answer; who may do what to which kind of thing
/// is the developer's, who changes the rights roles carry or registers another implementation. A route asks it after it has loaded the
/// thing and read its scope, and decides nothing else by it. The default answer is <c>HierarchyScopeAuthority</c>: a right held at the
/// thing's scope or at a scope above it counts.
/// </remarks>
public interface IScopeAuthority
{
    /// <summary>
    /// Says whether the caller holds a right at a scope or above.
    /// </summary>
    /// <param name="right">The right asked for, <c>{resource}:{action}</c>.</param>
    /// <param name="scope">Where the thing sits.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the caller may; otherwise the reason, as a result that names the right and the scope it was asked at.</returns>
    Task<IGenericResult> May(string right, ThingScope scope, CancellationToken cancellationToken = default);
}
