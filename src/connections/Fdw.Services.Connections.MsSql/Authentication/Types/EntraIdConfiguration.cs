using System.Collections.Generic;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.MsSql.Results;

namespace Fdw.Services.Connections.MsSql.Authentication.Types;

/// <summary>
/// Microsoft Entra ID (Azure AD) Authentication.
/// KVP keys: AzureAdMode, ClientId, TenantId, SecretManagerName.
/// </summary>
[TypeOption(typeof(MsSqlAuthenticationTypes), "EntraId")]
public sealed class EntraIdConfiguration : MsSqlAuthenticationConfiguration
{
    private static readonly string[] Expected = ["AzureAdMode", "ClientId", "TenantId", "SecretManagerName"];

    /// <summary>Initializes a new instance of the <see cref="EntraIdConfiguration"/> class.</summary>
    public EntraIdConfiguration()
        : base("EntraId",
               "Microsoft Entra ID",
               "Authenticate using Microsoft Entra ID (Azure Active Directory)",
               Expected, [], requiresSecret: false)
    {
    }

    // Why a platform default: SqlClient names "Active Directory Default" as the Entra ID mode it takes when none is
    // chosen, so a connection that names no AzureAdMode asks for exactly that mode (FDW-3580).
    [PlatformDefault]
    private const string SqlClientDefaultMode = "DEFAULT";

    /// <inheritdoc/>
    /// <remarks>Refuses an AzureAdMode SqlClient does not take, so the connection is refused when it is saved.</remarks>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values)
        => BuildAuthFragment(values, resolvedPassword: null) is { IsSuccess: false } refused
            ? refused
            : GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
    {
        values.TryGetValue("AzureAdMode", out var azureAdMode);

        return ModeOf(azureAdMode) switch
        {
            "SERVICEPRINCIPAL" or "SPN" => BuildServicePrincipal(values, resolvedPassword),
            "INTERACTIVE" => GenericResult<string>.Success("Authentication=Active Directory Interactive;"),
            "MANAGEDIDENTITY" or "MSI" => GenericResult<string>.Success("Authentication=Active Directory Managed Identity;"),
            SqlClientDefaultMode => GenericResult<string>.Success("Authentication=Active Directory Default;"),
            _ => GenericResult<string>.Failure(
                MsSqlConnectionResultCodes.ByName("AzureAdModeUnknown"),
                ResultDetails.Create("AzureAdMode", azureAdMode!, "AuthenticationType", Name))
        };
    }

    // No mode, or an empty one, is SqlClient's own default mode, as UsesHostIdentity reads it.
    private static string ModeOf(string? azureAdMode)
        => string.IsNullOrEmpty(azureAdMode) ? SqlClientDefaultMode : azureAdMode.ToUpperInvariant();

    private static IGenericResult<string> BuildServicePrincipal(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
    {
        var fragment = "Authentication=Active Directory Service Principal;";
        if (values.TryGetValue("ClientId", out var clientId) && !string.IsNullOrEmpty(clientId))
            fragment += string.Format(CultureInfo.InvariantCulture, "User Id={0};", clientId);
        if (!string.IsNullOrEmpty(resolvedPassword))
            fragment += string.Format(CultureInfo.InvariantCulture, "Password={0};", resolvedPassword);
        return GenericResult<string>.Success(fragment);
    }

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc/>
    /// <remarks>
    /// A service principal signs in with its own client id and secret. Every other mode -- Default, Interactive,
    /// ManagedIdentity, and no mode at all, which <see cref="BuildAuthFragment"/> builds as Default -- takes the
    /// identity the host process has, so it is the host's.
    /// </remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values)
    {
        if (values is null || !values.TryGetValue("AzureAdMode", out var mode) || string.IsNullOrEmpty(mode))
            return true;

        return !string.Equals(mode, "SERVICEPRINCIPAL", System.StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, "SPN", System.StringComparison.OrdinalIgnoreCase);
    }
}
