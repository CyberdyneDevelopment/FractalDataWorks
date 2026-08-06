using System.Collections.Generic;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

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
        : base(3, "EntraId",
               "Microsoft Entra ID",
               "Authenticate using Microsoft Entra ID (Azure Active Directory)",
               Expected, [], [])
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
    {
        values.TryGetValue("AzureAdMode", out var azureAdMode);
        var mode = azureAdMode?.ToUpperInvariant() ?? "DEFAULT";

        return mode switch
        {
            "SERVICEPRINCIPAL" or "SPN" => BuildServicePrincipal(values, resolvedPassword),
            "INTERACTIVE" => GenericResult<string>.Success("Authentication=Active Directory Interactive;"),
            "MANAGEDIDENTITY" or "MSI" => GenericResult<string>.Success("Authentication=Active Directory Managed Identity;"),
            _ => GenericResult<string>.Success("Authentication=Active Directory Default;")
        };
    }

    private static IGenericResult<string> BuildServicePrincipal(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
    {
        var fragment = "Authentication=Active Directory Service Principal;";
        if (values.TryGetValue("ClientId", out var clientId) && !string.IsNullOrEmpty(clientId))
            fragment += string.Format(CultureInfo.InvariantCulture, "User Id={0};", clientId);
        if (!string.IsNullOrEmpty(resolvedPassword))
            fragment += string.Format(CultureInfo.InvariantCulture, "Password={0};", resolvedPassword);
        return GenericResult<string>.Success(fragment);
    }
}
