using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.PostgreSql.Authentication.Types;

/// <summary>
/// No authentication — relies on the server's own trust/peer configuration. No KVP keys required.
/// </summary>
[TypeOption(typeof(PostgreSqlAuthenticationTypes), "None")]
public sealed class NonePostgreSqlAuthentication : PostgreSqlAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="NonePostgreSqlAuthentication"/> class.</summary>
    public NonePostgreSqlAuthentication()
        : base(1, "None",
               "No Authentication",
               "No username/password fragment — relies on the server's trust/peer configuration",
               expectedProperties: [],
               requiredProperties: [],
               secretPropertyNames: [])
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success(string.Empty);
}
