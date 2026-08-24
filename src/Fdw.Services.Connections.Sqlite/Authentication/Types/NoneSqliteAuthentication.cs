using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.Connections.Sqlite.Authentication.Types;

/// <summary>
/// No SQLite authentication — a plain, unencrypted database file. No secret is resolved.
/// KVP keys: (none).
/// </summary>
[TypeOption(typeof(SqliteAuthenticationTypes), "None")]
public sealed class NoneSqliteAuthentication : SqliteAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="NoneSqliteAuthentication"/> class.</summary>
    public NoneSqliteAuthentication()
        : base(1, "None",
               "No Authentication",
               "Plain SQLite database file with no encryption password",
               expectedProperties: [],
               requiredProperties: [])
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values)
        => GenericResult.Success();

    /// <inheritdoc/>
    // Why: None needs no secret — return a null password. The connection string is built without a
    // Password= keyword.
    public override Task<IGenericResult<string?>> ResolvePassword(
        IReadOnlyDictionary<string, string?> values,
        IPlatformServiceProvider<ISecretManager> secretManagerProvider,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<string?>.Success(null));
}
