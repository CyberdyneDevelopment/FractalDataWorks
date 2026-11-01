using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Execution;

/// <summary>
/// Holds a token issued at the end of a redirect sign-in until the browser that started the sign-in
/// redeems it with a single-use code.
/// </summary>
/// <remarks>
/// <para>
/// A redirect sign-in ends on a callback the identity provider navigates the browser to. That
/// response cannot hand a different-origin console a token in its body, so the callback stores the
/// token here and redirects the browser back with a code; the console then redeems the code over a
/// plain request.
/// </para>
/// <para>
/// There is no read-without-redeem method, for the same reason <see cref="IAuthenticationExecutionStore"/>
/// has none: a code is single-use, and redeeming must be atomic. A missing, expired, and
/// already-redeemed code fail identically, so a caller cannot learn that a code was ever real.
/// </para>
/// </remarks>
public interface IAuthorizationCodeStore
{
    /// <summary>Stores <paramref name="token"/> and returns the single-use code that redeems it.</summary>
    /// <param name="flowName">The flow that issued the token. Its key encrypts the stored token.</param>
    /// <param name="token">The token the flow issued.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A cryptographically random, single-use, short-lived code.</returns>
    Task<IGenericResult<string>> Issue(string flowName, IssuedToken token, CancellationToken cancellationToken = default);

    /// <summary>Redeems <paramref name="code"/>, returning its token and issuing flow exactly once.</summary>
    /// <param name="code">The code handed to the browser by the callback.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<RedeemedCode>> Redeem(string code, CancellationToken cancellationToken = default);
}
