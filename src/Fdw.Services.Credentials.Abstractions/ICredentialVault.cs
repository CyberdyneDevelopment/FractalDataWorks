using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Credentials.Abstractions.Outcomes;
using Fdw.Services.DataVault.Abstractions;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Narrow per-domain interface for the password-credential vault. These semantic verbs ARE the access
/// policy — there is no generic command surface. Inputs are ALREADY-DERIVED hashes: the service edge
/// runs the KDF on arrival so plaintext never crosses the vault boundary (DataVault README §4). No verb
/// returns stored hash/salt material.
/// </summary>
public interface ICredentialVault : IDataVault
{
    /// <summary>
    /// Compares a presented derived hash against the user's current stored secret in constant time,
    /// doing the SAME work on the negative path (decoy compare) so timing does not enumerate accounts
    /// (README §6). Produces only <c>Match</c>/<c>NoMatch</c>; the edge composes policy outcomes.
    /// </summary>
    /// <param name="userId">The user whose current secret to compare against.</param>
    /// <param name="derivedHash">The KDF output of the presented credential (never plaintext).</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ICredentialOutcome>> Validate(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new current secret for the user (version-on-write: retire any prior current row,
    /// insert the new peppered hash).
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="derivedHash">The KDF output to pepper and store (never plaintext).</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Create(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the user's current secret against <paramref name="oldDerivedHash"/> and, on a match,
    /// version-on-writes the new secret. Fails loud if the old hash does not match.
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="oldDerivedHash">The KDF output of the current credential, verified first.</param>
    /// <param name="newDerivedHash">The KDF output of the new credential to store.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Change(Guid userId, byte[] oldDerivedHash, byte[] newDerivedHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-retires the user's current secret (no current secret remains; no verb returns it).
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Disable(Guid userId, CancellationToken cancellationToken = default);
}
