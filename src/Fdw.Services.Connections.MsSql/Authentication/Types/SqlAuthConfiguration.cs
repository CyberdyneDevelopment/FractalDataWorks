using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

namespace Fdw.Services.Connections.MsSql.Authentication.Types;

/// <summary>
/// SQL Server Authentication — username and password.
/// KVP keys: Username, SecretKeyName, SecretManagerName.
/// </summary>
[TypeOption(typeof(MsSqlAuthenticationTypes), "SqlAuth")]
public sealed class SqlAuthConfiguration : MsSqlAuthenticationConfiguration
{
    private static readonly string[] Expected = ["Username", "SecretKeyName", "SecretManagerName"];
    private static readonly string[] Required = ["Username", "SecretKeyName"];
    private static readonly string[] Secret = ["SecretKeyName"];

    /// <summary>Initializes a new instance of the <see cref="SqlAuthConfiguration"/> class.</summary>
    public SqlAuthConfiguration()
        : base(1, "SqlAuth",
               "SQL Server Authentication",
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
            errors.Add("Username is required for SqlAuth");
        if (string.IsNullOrEmpty(secretKeyName))
            errors.Add("SecretKeyName is required for SqlAuth");

        return errors.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(
                MsSqlResultCodes.ByName("AuthenticationValidationFailed"),
                ResultDetails.Create("ValidationErrors", string.Join("; ", errors)));
    }

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<string>();

        values.TryGetValue("Username", out var username);
        var fragment = string.Format(CultureInfo.InvariantCulture, "User Id={0};", username);
        if (!string.IsNullOrEmpty(resolvedPassword))
            fragment += string.Format(CultureInfo.InvariantCulture, "Password={0};", resolvedPassword);

        return GenericResult<string>.Success(fragment);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// SecretManagerName and SecretKeyName are this type's own properties, so naming the store and
    /// the key belongs here rather than in whatever builds the connection. Naming the manager rather
    /// than accepting one is also what makes a silent credential substitution impossible: the
    /// provider reads the store this connection declared and has no way to prefer a different one.
    /// </remarks>
    public override IGenericResult<SecretRequirement> RequiredSecret(IReadOnlyDictionary<string, string?> values)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<SecretRequirement>();

        values.TryGetValue("SecretKeyName", out var secretKeyName);
        if (string.IsNullOrEmpty(secretKeyName))
            return GenericResult<SecretRequirement>.Success(SecretRequirement.None);

        // A declared key with no store to read it from is a configuration defect, not "no secret".
        var declared = GetValue(values, "SecretManagerName");
        return declared.IsSuccess
            ? GenericResult<SecretRequirement>.Success(new SecretRequirement(declared.Value!, secretKeyName!))
            : declared.ToNewResult<SecretRequirement>();
    }
}
