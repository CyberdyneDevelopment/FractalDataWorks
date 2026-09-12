using Fdw.Services.Connections.Abstractions;
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
    /// <remarks>
    /// SecretManagerName and SecretKeyName are this method's own properties, so naming the store
    /// and the key belongs here. Naming the manager rather than accepting one is what makes a
    /// silent credential substitution impossible.
    /// </remarks>
    public override IGenericResult<SecretRequirement> RequiredSecret(
        IReadOnlyDictionary<string, string?> values)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<SecretRequirement>();

        values.TryGetValue("SecretManagerName", out var secretManagerName);
        values.TryGetValue("SecretKeyName", out var secretKeyName);

        // Validate has already established both are present for this method; a missing one here is
        // a defect in Validate, not something to paper over with a default.
        return string.IsNullOrEmpty(secretManagerName) || string.IsNullOrEmpty(secretKeyName)
            ? GenericResult<SecretRequirement>.Failure(
                SqliteDataResultCodes.ByName("AuthenticationValidationFailed"),
                ResultDetails.Create(
                    "ValidationErrors",
                    "EncryptionKey authentication requires both SecretManagerName and SecretKeyName."))
            : GenericResult<SecretRequirement>.Success(
                new SecretRequirement(secretManagerName!, secretKeyName!));
    }
}
