using Fdw.Services.Connections.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;

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
        : base("None",
               "No Authentication",
               "Plain SQLite database file with no encryption password",
               expectedProperties: [],
               requiredProperties: [],
               requiresSecret: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values)
        => GenericResult.Success();

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override IGenericResult<SecretRequirement> RequiredSecret(
        IReadOnlyDictionary<string, string?> values)
        => GenericResult<SecretRequirement>.Success(SecretRequirement.None);

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc/>
    /// <remarks>The database is a file the host process opens with its own file-system access.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => true;
}
