using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// Marker interface for a data vault — a sealed, write-and-compare secret store for
/// user-supplied secrets that must be verified but never retrieved (password hashes,
/// personal-access-token hashes, agent-key hashes, later payment tokens).
/// </summary>
/// <remarks>
/// <para>
/// A vault exposes <b>no command surface</b>. The dangerous capabilities (the live connection,
/// the pepper, the constant-time compare) live inside the vault implementation
/// (<c>DataVaultBase</c>) and are never handed to code authored elsewhere. The provider
/// (<see cref="IDataVaultProvider"/>) resolves a vault as <see cref="IDataVault"/>; consumers
/// cast it to a narrow per-domain interface (e.g. <c>ICredentialVault</c>) whose semantic verbs
/// (Validate/Create/Change/Disable) ARE the access-control policy. No verb returns stored secret
/// material, so a hash can never leave the vault.
/// </para>
/// <para>
/// System/app secrets (connection passwords, the pepper/HMAC key) are NOT vault material — they
/// belong to the SecretManager domain. The vault is for user-stored verify-only secrets.
/// </para>
/// </remarks>
public interface IDataVault : IServiceOption
{
}
