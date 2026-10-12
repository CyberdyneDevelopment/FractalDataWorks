using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Maps a subject an external authority asserted to a principal here.
/// </summary>
/// <remarks>
/// Keyed on the pair, because a subject identifier is unique only within its issuer. Never on an
/// email address.
/// </remarks>
public interface IPrincipalBinding
{
    /// <summary>Returns the principal bound to this pair, or null when none is.</summary>
    /// <param name="issuer">The authority that asserted the subject.</param>
    /// <param name="subjectId">The subject identifier, unique within that issuer.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// A successful result with a null value means "no binding exists", which is a different
    /// question from "the lookup failed" — provisioning policy acts on the first and not the second.
    /// </remarks>
    Task<IGenericResult<Principal?>> Resolve(
        string issuer, string subjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeems an operator's open invitation for <paramref name="verifiedEmail"/> at this issuer: the
    /// invitation becomes a binding to <paramref name="subjectId"/>, and its principal is returned.
    /// </summary>
    /// <param name="issuer">The authority that asserted the subject.</param>
    /// <param name="subjectId">The subject identifier the issuer proved.</param>
    /// <param name="verifiedEmail">An address the caller has established the issuer verified.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Success with null means no open invitation matches, which a caller treats exactly like an
    /// unbound subject. An invitation is used once: redeemed, it names its subject and is never matched
    /// by address again.
    /// </remarks>
    Task<IGenericResult<Principal?>> RedeemInvitation(
        string issuer, string subjectId, string verifiedEmail, CancellationToken cancellationToken = default);
}
