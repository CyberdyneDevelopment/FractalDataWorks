using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.SecretManagers.HashiCorpVault.Auth;

/// <summary>
/// Authenticates with Vault's JWT method — presenting an assertion an external issuer already minted
/// for this workload (a CI job's OIDC token, a projected service-account token).
/// </summary>
/// <remarks>
/// The only method here with no static secret at rest, and the one to prefer wherever the workload
/// already has a trustworthy issuer. It is the same trust model the managed identity domain's
/// federated mechanism uses, pointed at Vault instead of Authentik.
/// </remarks>
[TypeOption(typeof(VaultAuthMethods), "Jwt")]
public sealed class JwtVaultAuthMethod : VaultAuthMethodBase
{
    /// <summary>Initializes a new instance of the <see cref="JwtVaultAuthMethod"/> class.</summary>
    public JwtVaultAuthMethod() : base(3, "Jwt", defaultMount: "jwt", requiresStoredSecret: false)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, string> BuildLoginPayload(string? roleId, string secret)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["role"] = roleId ?? string.Empty,
            ["jwt"] = secret,
        };
}
