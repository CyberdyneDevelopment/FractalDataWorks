using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>
/// One-time links that let whoever holds them set a user's password (FDW-941). An administrator creates
/// a link and shares it or has it emailed; the person it was made for opens it and chooses a password.
/// </summary>
/// <remarks>
/// The raw token is minted here, returned once, and only its peppered hash is stored -- the same
/// contract as <see cref="IPersonalAccessTokenService"/>. A link works once, until it expires, and only
/// until a newer link is made for the same user.
/// </remarks>
public interface IPasswordLinkService
{
    /// <summary>Creates a link for <paramref name="userId"/>, revoking any earlier unused one.</summary>
    /// <param name="userId">The user whose password the link sets.</param>
    /// <param name="createdBy">The administrator creating it.</param>
    /// <param name="expiresAt">When the link stops working.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The raw token, returned this once, and the link's id and expiry.</returns>
    Task<IGenericResult<PasswordLinkCreatedResult>> CreateLink(
        Guid userId, Guid createdBy, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>Uses up the live link <paramref name="rawToken"/> names, if there is one.</summary>
    /// <param name="rawToken">The token from the link.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>
    /// Success carrying whether the token named a live link and, if so, whose. An unknown, used, revoked
    /// or expired token is a success with <c>IsValid</c> false -- the caller refuses it without saying
    /// which, so a guesser learns nothing.
    /// </returns>
    Task<IGenericResult<PasswordLinkRedemption>> Redeem(string rawToken, CancellationToken cancellationToken = default);
}
