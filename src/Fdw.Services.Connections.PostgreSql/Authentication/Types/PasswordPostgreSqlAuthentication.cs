using System;
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
        ISecretManager? supplied,
        ISecretManagerProvider? provider,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string>();

        string? resolvedPassword = null;
        values.TryGetValue("SecretKeyName", out var secretKeyName);
        if (!string.IsNullOrEmpty(secretKeyName))
        {
            var manager = await Manager(values, supplied, provider, cancellationToken).ConfigureAwait(false);
            if (!manager.IsSuccess)
                return manager.ToNewResult<string>();

            if (manager.Value is not { } secretManager)
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

    /// <summary>Finds the manager holding this connection's password.</summary>
    /// <remarks>
    /// SecretManagerName is one of this type's own properties, so choosing the manager belongs here
    /// rather than in whatever is building the connection. A supplied manager still has to BE the
    /// store this connection named: reading a password out of a store the connection never declared
    /// is a silent credential substitution, so a mismatch is refused rather than preferred.
    /// </remarks>
    /// <param name="values">This type's own properties.</param>
    /// <param name="supplied">A manager handed in directly, if the caller held one.</param>
    /// <param name="provider">Resolves a manager by the name this connection declares.</param>
    /// <param name="cancellationToken">A token to cancel the resolution.</param>
    private static async Task<IGenericResult<ISecretManager?>> Manager(
        IReadOnlyDictionary<string, string?> values,
        ISecretManager? supplied,
        ISecretManagerProvider? provider,
        CancellationToken cancellationToken)
    {
        values.TryGetValue("SecretManagerName", out var declared);
        if (string.IsNullOrWhiteSpace(declared))
            return GenericResult<ISecretManager?>.Failure(
                PostgreSqlResultCodes.ByName("AuthenticationValidationFailed"),
                ResultDetails.Create(
                    "ValidationErrors",
                    "Password authentication declares a SecretKeyName but no SecretManagerName to read it from."));

        if (supplied is not null)
        {
            return string.Equals(supplied.Name, declared, StringComparison.OrdinalIgnoreCase)
                ? GenericResult<ISecretManager?>.Success(supplied)
                : GenericResult<ISecretManager?>.Failure(
                    PostgreSqlResultCodes.ByName("AuthenticationValidationFailed"),
                    ResultDetails.Create(
                        "ValidationErrors",
                        $"The connection declares secret manager '{declared}' but '{supplied.Name}' was supplied."));
        }

        if (provider is null)
            return GenericResult<ISecretManager?>.Failure(
                PostgreSqlResultCodes.ByName("AuthenticationValidationFailed"),
                ResultDetails.Create(
                    "ValidationErrors",
                    $"Password authentication needs secret manager '{declared}' and no provider was available to resolve it."));

        var resolved = await provider.Get(declared, cancellationToken).ConfigureAwait(false);
        return resolved.IsSuccess && resolved.Value is not null
            ? GenericResult<ISecretManager?>.Success(resolved.Value)
            : resolved.ToNewResult<ISecretManager?>();
    }
}
