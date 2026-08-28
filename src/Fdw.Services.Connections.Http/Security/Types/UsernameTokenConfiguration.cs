using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// WS-Security UsernameToken authentication configuration.
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "UsernameToken")]
public sealed class UsernameTokenConfiguration : HttpAuthenticationConfiguration
{
    private static readonly string[] ExpectedList = ["SecretManagerName", "UsernameSecretName", "PasswordSecretName"];
    private static readonly string[] RequiredList = ["SecretManagerName", "UsernameSecretName", "PasswordSecretName"];

    /// <summary>Initializes a new instance of the <see cref="UsernameTokenConfiguration"/> class.</summary>
    public UsernameTokenConfiguration()
        : base(3, "UsernameToken", "Username Token", "WS-Security UsernameToken authentication",
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
    public override HttpAuthenticationConfiguration CreateInstance() => new UsernameTokenConfiguration();

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
