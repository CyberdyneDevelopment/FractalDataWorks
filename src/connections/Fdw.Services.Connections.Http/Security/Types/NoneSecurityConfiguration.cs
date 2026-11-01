using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// No security applied — pass-through configuration.
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "None")]
public sealed class NoneSecurityConfiguration : HttpAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="NoneSecurityConfiguration"/> class.</summary>
    public NoneSecurityConfiguration()
        : base("None", "No Security", "No authentication or message security applied", requiresSecret: false)
    {
    }

    /// <inheritdoc/>
    public override HttpAuthenticationConfiguration CreateInstance() => new NoneSecurityConfiguration();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return [new("Type", Name)];
    }

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc />
    public override HttpSecretNames SecretNames => HttpSecretNames.None;

    /// <inheritdoc/>
    /// <remarks>No security presents no identity at all; the request is anonymous.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => false;
}
