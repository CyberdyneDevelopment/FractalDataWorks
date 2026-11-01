using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// Bearer token authentication configuration.
/// The token is read from <see cref="TokenSecretName"/> in the secret manager the connection names, and
/// every request carries it as <c>Authorization: Bearer &lt;token&gt;</c>.
/// </summary>
/// <remarks>
/// The token travels in the protocol context's resolved key, the same slot an API key uses; the
/// connection's <c>AuthenticationType</c> decides which header that key goes out in.
/// </remarks>
[TypeOption(typeof(HttpAuthenticationTypes), "Bearer")]
public sealed class BearerSecurityConfiguration : HttpAuthenticationConfiguration
{
    private static readonly string[] ExpectedList = ["SecretManagerName", "TokenSecretName"];
    private static readonly string[] RequiredList = ["SecretManagerName", "TokenSecretName"];

    /// <summary>Initializes a new instance of the <see cref="BearerSecurityConfiguration"/> class.</summary>
    public BearerSecurityConfiguration()
        : base("Bearer", "Bearer Token", "Bearer token read from a secret and sent in the Authorization header",
               ExpectedList, RequiredList, requiresSecret: true)
    {
    }

    /// <summary>
    /// Gets or sets the name of the secret containing the bearer token.
    /// </summary>
    public string? TokenSecretName { get; set; }

    /// <inheritdoc/>
    public override HttpAuthenticationConfiguration CreateInstance() => new BearerSecurityConfiguration();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
        TokenSecretName = section[nameof(TokenSecretName)];
    }

    /// <inheritdoc/>
    public override void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
        values.TryGetValue(nameof(TokenSecretName), out var token);
        TokenSecretName = token;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return
        [
            new("Type", Name),
            new(nameof(TokenSecretName), TokenSecretName),
        ];
    }

    /// <inheritdoc />
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [new("TokenSecretName", "Bearer token", secret: true)];

    /// <inheritdoc />
    public override HttpSecretNames SecretNames => new(null, null, TokenSecretName);

    /// <inheritdoc/>
    /// <remarks>The connection carries its own token.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => false;
}
