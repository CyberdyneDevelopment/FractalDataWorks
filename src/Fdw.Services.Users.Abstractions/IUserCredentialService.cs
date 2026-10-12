using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Credentials.Abstractions.Outcomes;

namespace Fdw.Services.Users.Abstractions;

/// <summary>
/// Service for managing user credentials stored in auth.UserSecret.
/// Handles hashing at the service boundary — plaintext never leaves these methods.
/// </summary>
public interface IUserCredentialService
{
    /// <summary>
    /// Verifies a plaintext credential against the stored hash for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="secretType">The secret type (currently only "Password" is vault-backed).</param>
    /// <param name="plaintext">The plaintext value to verify (hashed on arrival; never retained).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Success carrying the composed <see cref="ICredentialOutcome"/> (Match=Valid / NoMatch / Expired /
    /// MustChange / TooManyAttempts), or a structured failure on a system error. Callers proceed only on
    /// an outcome whose <see cref="ICredentialOutcome.GrantsAccess"/> is true.
    /// </returns>
    Task<IGenericResult<ICredentialOutcome>> Verify(
        Guid userId,
        string secretType,
        string plaintext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hashes and stores a new credential for a user. Deactivates any existing current credential
    /// of the same type via version-on-write.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="secretType">The secret type.</param>
    /// <param name="plaintext">The plaintext value to hash and store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult> Store(
        Guid userId,
        string secretType,
        string plaintext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the user's current credential of the given type, so it can no longer be used to sign in.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="secretType">The secret type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// The credential is disabled in the vault rather than deleted, so its history stays. The account
    /// keeps every other way in: an external identity or an API key is untouched.
    /// </remarks>
    Task<IGenericResult> Remove(
        Guid userId,
        string secretType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the MustChangePasswordOnLogin flag on a user.
    /// </summary>
    Task<IGenericResult> ForcePasswordChange(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers whether the user currently holds a credential of the given type.
    /// </summary>
    /// <remarks>
    /// Presence only. Granting a FIRST password and resetting an existing one are different acts
    /// with different authority, so the caller has to be able to tell them apart without reading
    /// anything the vault stores.
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="secretType">The secret type (currently only "Password" is vault-backed).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<bool>> HasCredential(
        Guid userId,
        string secretType,
        CancellationToken cancellationToken = default);
}
