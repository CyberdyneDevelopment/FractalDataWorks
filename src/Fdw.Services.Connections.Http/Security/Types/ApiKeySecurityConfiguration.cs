using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// API key authentication configuration.
/// The API key is passed in a request header (default: <see cref="HttpAuthenticationTypes.ApiKeyHeaderName"/>).
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "ApiKey")]
public sealed class ApiKeySecurityConfiguration : HttpAuthenticationConfiguration
{
    private static readonly string[] ExpectedList = ["SecretManagerName", "ApiKeySecretName", "ApiKeyHeaderName"];
    private static readonly string[] RequiredList = ["SecretManagerName", "ApiKeySecretName"];

    /// <summary>Initializes a new instance of the <see cref="ApiKeySecurityConfiguration"/> class.</summary>
    public ApiKeySecurityConfiguration()
        : base("ApiKey", "API Key", "API key passed in a request header",
               ExpectedList, RequiredList, requiresSecret: true)
    {
    }

    /// <summary>
    /// Gets or sets the name of the secret containing the API key value.
    /// </summary>
    public string? ApiKeySecretName { get; set; }

    /// <summary>
    /// Gets or sets the header name for the API key.
    /// When null, the key is sent in <see cref="HttpAuthenticationTypes.ApiKeyHeaderName"/>.
    /// </summary>
    public string? ApiKeyHeaderName { get; set; }

    /// <inheritdoc/>
    public override HttpAuthenticationConfiguration CreateInstance() => new ApiKeySecurityConfiguration();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
        ApiKeySecretName = section[nameof(ApiKeySecretName)];
        ApiKeyHeaderName = section[nameof(ApiKeyHeaderName)];
    }

    /// <inheritdoc/>
    public override void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
        values.TryGetValue(nameof(ApiKeySecretName), out var key);
        ApiKeySecretName = key;
        values.TryGetValue(nameof(ApiKeyHeaderName), out var header);
        ApiKeyHeaderName = header;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return
        [
            new("Type", Name),
            new(nameof(ApiKeySecretName), ApiKeySecretName),
            new(nameof(ApiKeyHeaderName), ApiKeyHeaderName),
        ];
    }

    /// <inheritdoc />
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [new("ApiKeySecretName", "API key", secret: true)];

    /// <inheritdoc/>
    /// <remarks>The connection carries its own API key.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => false;
}
