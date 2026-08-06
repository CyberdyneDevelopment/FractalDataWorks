using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Credentials.Abstractions.Outcomes;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// A credential service — a configured, named indirection in front of a credential
/// <see cref="ICredentialVault"/>. Consumers (the Users edge) resolve a credential service by name and
/// call its semantic verbs, exactly as connections resolve a secret manager by name. The credential
/// service owns which vault its verbs run against; it forwards to that vault and never sees hash material.
/// </summary>
/// <remarks>
/// <para>
/// The surface is the SAME closed set of semantic verbs as <see cref="ICredentialVault"/> — there is
/// no command surface. Inputs are ALREADY-DERIVED hashes (the edge runs the KDF on arrival); plaintext
/// never crosses this boundary, and no verb returns stored hash/salt material.
/// </para>
/// </remarks>
public interface ICredentialService : IServiceOption
{
    /// <summary>
    /// Validates a presented derived hash for a user against the configured vault.
    /// </summary>
    /// <param name="userId">The user whose current secret to compare against.</param>
    /// <param name="derivedHash">The KDF output of the presented credential (never plaintext).</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ICredentialOutcome>> Validate(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new current secret for the user (version-on-write) via the configured vault.
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="derivedHash">The KDF output to pepper and store (never plaintext).</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Create(Guid userId, byte[] derivedHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the user's current secret then version-on-writes the new secret via the configured vault.
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="oldDerivedHash">The KDF output of the current credential, verified first.</param>
    /// <param name="newDerivedHash">The KDF output of the new credential to store.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Change(Guid userId, byte[] oldDerivedHash, byte[] newDerivedHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-retires the user's current secret via the configured vault.
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Disable(Guid userId, CancellationToken cancellationToken = default);
}
