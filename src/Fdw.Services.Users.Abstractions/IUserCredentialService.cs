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
    /// Sets the MustChangePasswordOnLogin flag on a user.
    /// </summary>
    Task<IGenericResult> ForcePasswordChange(
        Guid userId,
        CancellationToken cancellationToken = default);
}
