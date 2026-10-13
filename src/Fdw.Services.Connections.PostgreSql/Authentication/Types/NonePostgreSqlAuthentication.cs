using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.PostgreSql.Authentication.Types;

/// <summary>
/// No authentication — relies on the server's own trust/peer configuration. No KVP keys required.
/// </summary>
[TypeOption(typeof(PostgreSqlAuthenticationTypes), "None")]
public sealed class NonePostgreSqlAuthentication : PostgreSqlAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="NonePostgreSqlAuthentication"/> class.</summary>
    public NonePostgreSqlAuthentication()
        : base("None",
               "No Authentication",
               "No username/password fragment — relies on the server's trust/peer configuration",
               expectedProperties: [],
               requiredProperties: [],
               requiresSecret: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success(string.Empty);

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc/>
    /// <remarks>With no credential the server admits the connection on trust or peer rules, which identify the host and the account it runs as.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => true;
}
