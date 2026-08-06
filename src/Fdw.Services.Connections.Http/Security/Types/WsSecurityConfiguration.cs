using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// WS-Security with X.509 certificate signing configuration.
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "WsSecurity")]
public sealed class WsSecurityConfiguration : HttpAuthenticationConfiguration
{
    // Why: SecretManagerName is a key THIS secret-backed method owns and requires (it resolves the
    // certificate secret name). It is NOT on the base — non-secret methods declare no such key.
    private static readonly string[] ExpectedList = ["SecretManagerName", "CertificateSecretName", "TimestampTtlSeconds"];
    private static readonly string[] RequiredList = ["SecretManagerName", "CertificateSecretName"];

    /// <summary>Initializes a new instance of the <see cref="WsSecurityConfiguration"/> class.</summary>
    public WsSecurityConfiguration()
        : base(2, "WsSecurity", "WS-Security", "WS-Security with X.509 certificate signing",
               ExpectedList, RequiredList)
    {
    }

    /// <summary>
    /// Gets or sets the name of the secret containing the X.509 certificate (PFX format).
    /// </summary>
    public string? CertificateSecretName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp time-to-live in seconds.
    /// Stored as a string in the KVP table; parsed by the consumer.
    /// </summary>
    public string? TimestampTtlSeconds { get; set; }

    /// <summary>
    /// Gets the parsed timestamp TTL value in seconds, defaulting to 300 if not set.
    /// </summary>
    public int TimestampTtlSecondsValue =>
        int.TryParse(TimestampTtlSeconds, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : 300;

    /// <inheritdoc/>
    public override HttpAuthenticationConfiguration CreateInstance() => new WsSecurityConfiguration();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
        CertificateSecretName = section[nameof(CertificateSecretName)];
        TimestampTtlSeconds = section[nameof(TimestampTtlSeconds)];
    }

    /// <inheritdoc/>
    public override void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
        values.TryGetValue(nameof(CertificateSecretName), out var cert);
        CertificateSecretName = cert;
        values.TryGetValue(nameof(TimestampTtlSeconds), out var ttl);
        TimestampTtlSeconds = ttl;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return
        [
            new("Type", Name),
            new(nameof(CertificateSecretName), CertificateSecretName),
            new(nameof(TimestampTtlSeconds), TimestampTtlSeconds),
        ];
    }
}
