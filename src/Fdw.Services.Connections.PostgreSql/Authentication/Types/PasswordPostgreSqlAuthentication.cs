using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.PostgreSql.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

namespace Fdw.Services.Connections.PostgreSql.Authentication.Types;

/// <summary>
/// Username/password authentication.
/// KVP keys: Username, SecretKeyName, SecretManagerName.
/// </summary>
[TypeOption(typeof(PostgreSqlAuthenticationTypes), "Password")]
public sealed class PasswordPostgreSqlAuthentication : PostgreSqlAuthenticationConfiguration
{
    private static readonly string[] Expected = ["Username", "SecretKeyName", "SecretManagerName"];
    private static readonly string[] Required = ["Username", "SecretKeyName"];
    private static readonly string[] Secret = ["SecretKeyName"];

    /// <summary>Initializes a new instance of the <see cref="PasswordPostgreSqlAuthentication"/> class.</summary>
    public PasswordPostgreSqlAuthentication()
        : base(2, "Password",
               "Username/Password Authentication",
               "Username and password authentication",
               Expected, Required, Secret)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values)
    {
        var errors = new List<string>();
        values.TryGetValue("Username", out var username);
        values.TryGetValue("SecretKeyName", out var secretKeyName);
        if (string.IsNullOrEmpty(username))
            errors.Add("Username is required for Password authentication");
        if (string.IsNullOrEmpty(secretKeyName))
            errors.Add("SecretKeyName is required for Password authentication");

        return errors.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(
                PostgreSqlResultCodes.ByName("AuthenticationValidationFailed"),
                ResultDetails.Create("ValidationErrors", string.Join("; ", errors)));
    }

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string>();

        values.TryGetValue("Username", out var username);
        var fragment = string.Format(CultureInfo.InvariantCulture, "Username={0};", username);
        if (!string.IsNullOrEmpty(resolvedPassword))
            fragment += string.Format(CultureInfo.InvariantCulture, "Password={0};", resolvedPassword);

        return GenericResult<string>.Success(fragment);
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<string>> BuildAuthFragment(
        IReadOnlyDictionary<string, string?> values,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string>();

        string? resolvedPassword = null;
        values.TryGetValue("SecretKeyName", out var secretKeyName);
        if (!string.IsNullOrEmpty(secretKeyName))
        {
            if (secretManager is null)
            {
                return GenericResult<string>.Failure(
                    PostgreSqlResultCodes.ByName("AuthenticationValidationFailed"),
                    ResultDetails.Create("ValidationErrors",
                        "Password authentication requires a SecretManager to resolve SecretKeyName but none was supplied."));
            }
            var secretCommand = GetSecretManagerCommand.Latest(null, secretKeyName);
            var secretResult = await secretManager.Execute(secretCommand, cancellationToken).ConfigureAwait(false);
            if (!secretResult.IsSuccess || secretResult.Value is null)
                return secretResult.ToNewResult<string>();
            resolvedPassword = secretResult.Value.GetStringValue();
        }

        return BuildAuthFragment(values, resolvedPassword);
    }
}
