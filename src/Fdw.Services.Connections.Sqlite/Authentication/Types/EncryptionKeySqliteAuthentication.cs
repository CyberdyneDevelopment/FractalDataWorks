using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Data.Sqlite.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

namespace Fdw.Services.Connections.Sqlite.Authentication.Types;

/// <summary>
/// Encryption-key SQLite authentication — an encrypted database file (SQLCipher/SEE) whose key is
/// resolved from a secret manager and injected as the <c>Password=</c> connection-string keyword.
/// KVP keys: SecretKeyName, SecretManagerName.
/// </summary>
[TypeOption(typeof(SqliteAuthenticationTypes), "EncryptionKey")]
public sealed class EncryptionKeySqliteAuthentication : SqliteAuthenticationConfiguration
{
    private static readonly string[] Expected = ["SecretKeyName", "SecretManagerName"];
    private static readonly string[] Required = ["SecretKeyName", "SecretManagerName"];

    /// <summary>Initializes a new instance of the <see cref="EncryptionKeySqliteAuthentication"/> class.</summary>
    public EncryptionKeySqliteAuthentication()
        : base(2, "EncryptionKey",
               "Encryption Key",
               "Encrypted SQLite database whose key is resolved from a secret manager",
               Expected, Required)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values)
    {
        var errors = new List<string>();
        values.TryGetValue("SecretKeyName", out var secretKeyName);
        values.TryGetValue("SecretManagerName", out var secretManagerName);
        if (string.IsNullOrEmpty(secretKeyName))
            errors.Add("SecretKeyName is required for EncryptionKey authentication");
        if (string.IsNullOrEmpty(secretManagerName))
            errors.Add("SecretManagerName is required for EncryptionKey authentication");

        return errors.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(
                SqliteDataResultCodes.ByName("AuthenticationValidationFailed"),
                ResultDetails.Create("ValidationErrors", string.Join("; ", errors)));
    }

    /// <inheritdoc/>
    // Why: EncryptionKey owns the SecretManagerName/SecretKeyName keys — it reads them from `values`,
    // resolves the named manager via the FDW provider, and reads the secret. No "Default" fallback:
    // a missing manager name fails validation above.
    public override async Task<IGenericResult<string?>> ResolvePassword(
        IReadOnlyDictionary<string, string?> values,
        IFdwServiceProvider<ISecretManager> secretManagerProvider,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string?>();

        values.TryGetValue("SecretManagerName", out var secretManagerName);
        values.TryGetValue("SecretKeyName", out var secretKeyName);

        var managerResult = await secretManagerProvider.Get(secretManagerName!, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<string?>.Failure(
                SqliteDataResultCodes.ByName("AuthenticationValidationFailed"),
                ResultDetails.Create("ValidationErrors", $"Secret manager '{secretManagerName}' could not be resolved."));

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(null, secretKeyName!), cancellationToken)
            .ConfigureAwait(false);
        if (!secretResult.IsSuccess || secretResult.Value is null)
            return secretResult.ToNewResult<string?>();

        return GenericResult<string?>.Success(secretResult.Value.GetStringValue());
    }
}
