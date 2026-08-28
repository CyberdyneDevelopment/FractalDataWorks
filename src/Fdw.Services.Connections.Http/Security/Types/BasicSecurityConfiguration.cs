using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// HTTP Basic authentication configuration.
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "Basic")]
public sealed class BasicSecurityConfiguration : HttpAuthenticationConfiguration
{
    private static readonly string[] ExpectedList = ["SecretManagerName", "UsernameSecretName", "PasswordSecretName"];
    private static readonly string[] RequiredList = ["SecretManagerName", "UsernameSecretName", "PasswordSecretName"];

    /// <summary>Initializes a new instance of the <see cref="BasicSecurityConfiguration"/> class.</summary>
    public BasicSecurityConfiguration()
        : base(5, "Basic", "HTTP Basic", "HTTP Basic authentication (username and password)",
               ExpectedList, RequiredList)
    {
    }

    /// <summary>
    /// Gets or sets the name of the secret containing the username.
    /// </summary>
    public string? UsernameSecretName { get; set; }

    /// <summary>
    /// Gets or sets the name of the secret containing the password.
    /// </summary>
    public string? PasswordSecretName { get; set; }

    /// <inheritdoc/>
    public override HttpAuthenticationConfiguration CreateInstance() => new BasicSecurityConfiguration();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
        UsernameSecretName = section[nameof(UsernameSecretName)];
        PasswordSecretName = section[nameof(PasswordSecretName)];
    }

    /// <inheritdoc/>
    public override void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
        values.TryGetValue(nameof(UsernameSecretName), out var usr);
        UsernameSecretName = usr;
        values.TryGetValue(nameof(PasswordSecretName), out var pwd);
        PasswordSecretName = pwd;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return
        [
            new("Type", Name),
            new(nameof(UsernameSecretName), UsernameSecretName),
            new(nameof(PasswordSecretName), PasswordSecretName),
        ];
    }
}
