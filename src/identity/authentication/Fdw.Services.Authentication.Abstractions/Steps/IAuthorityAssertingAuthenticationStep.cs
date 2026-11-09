using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A step whose provable methods are the ones its authority permits, read when asked and not kept (FDW-4697).
/// </summary>
/// <remarks>
/// <para>
/// Why: sign-in admission and flow administration decide from the methods a flow's steps can prove, before any login has run. A step that relays a foreign authority cannot state
/// them as a fixed list, and the step is one instance for the process, so it must not remember what the last login read. It reads its authority when asked and answers.
/// </para>
/// <para>
/// An authority that cannot be read is a failure that says which authority and why, and the caller refuses with that reason; empty means only that the authority was read and permits
/// this step to assert nothing.
/// </para>
/// </remarks>
public interface IAuthorityAssertingAuthenticationStep : IAuthenticationStep
{
    /// <summary>Reads the authority and returns the methods it permits this step to assert.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The methods the authority permits, empty when it permits none, or a failure with its own coded message when the authority cannot be read.</returns>
    Task<IGenericResult<IReadOnlyList<string>>> ProvableMethods(CancellationToken cancellationToken = default);
}
