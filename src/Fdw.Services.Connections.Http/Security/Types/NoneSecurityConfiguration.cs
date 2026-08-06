using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Connections.Http.Security.Types;

/// <summary>
/// No security applied — pass-through configuration.
/// </summary>
[TypeOption(typeof(HttpAuthenticationTypes), "None")]
public sealed class NoneSecurityConfiguration : HttpAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="NoneSecurityConfiguration"/> class.</summary>
    public NoneSecurityConfiguration()
        : base(1, "None", "No Security", "No authentication or message security applied")
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
}
