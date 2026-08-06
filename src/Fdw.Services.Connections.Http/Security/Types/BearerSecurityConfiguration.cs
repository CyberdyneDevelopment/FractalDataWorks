using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// Bearer token authentication configuration.
/// Placeholder for future token-based authentication support.
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "Bearer")]
public sealed class BearerSecurityConfiguration : HttpAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="BearerSecurityConfiguration"/> class.</summary>
    public BearerSecurityConfiguration()
        : base(6, "Bearer", "Bearer Token", "Bearer token authentication")
    {
    }

    /// <inheritdoc/>
    public override HttpAuthenticationConfiguration CreateInstance() => new BearerSecurityConfiguration();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return [new("Type", Name)];
    }
}
