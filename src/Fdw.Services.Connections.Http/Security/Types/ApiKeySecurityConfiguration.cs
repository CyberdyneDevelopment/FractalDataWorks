using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// API key authentication configuration.
/// The API key is passed in a request header (default: X-API-Key).
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "ApiKey")]
public sealed class ApiKeySecurityConfiguration : HttpAuthenticationConfiguration
{
    // Why: SecretManagerName is a key THIS secret-backed method owns and requires (it resolves the
    // API-key secret name). It is NOT on the base — non-secret methods declare no such key.
    private static readonly string[] ExpectedList = ["SecretManagerName", "ApiKeySecretName", "ApiKeyHeaderName"];
    private static readonly string[] RequiredList = ["SecretManagerName", "ApiKeySecretName"];

    /// <summary>Initializes a new instance of the <see cref="ApiKeySecurityConfiguration"/> class.</summary>
    public ApiKeySecurityConfiguration()
        : base(4, "ApiKey", "API Key", "API key passed in a request header",
               ExpectedList, RequiredList)
    {
    }

    /// <summary>
    /// Gets or sets the name of the secret containing the API key value.
    /// </summary>
    public string? ApiKeySecretName { get; set; }

    /// <summary>
    /// Gets or sets the header name for the API key.
    /// When null, consumers default to "X-API-Key".
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
}
