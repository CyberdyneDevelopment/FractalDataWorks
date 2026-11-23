using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>A service principal signs in with its own client id and secret.</summary>
[ExcludeFromCodeCoverage]
public abstract class ServicePrincipalAzureAdModeBase : AzureAdModeBase
{
    /// <summary>Initializes the mode with its name and the word it is found by.</summary>
    /// <param name="name">The mode's name.</param>
    /// <param name="word">The word that names the mode, in upper case.</param>
    protected ServicePrincipalAzureAdModeBase(string name, string word)
        : base(name, word, "Authentication=Active Directory Service Principal;")
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<string> Fragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
    {
        var fragment = "Authentication=Active Directory Service Principal;";
        if (values.TryGetValue("ClientId", out var clientId) && !string.IsNullOrEmpty(clientId))
            fragment += string.Format(CultureInfo.InvariantCulture, "User Id={0};", clientId);
        if (!string.IsNullOrEmpty(resolvedPassword))
            fragment += string.Format(CultureInfo.InvariantCulture, "Password={0};", resolvedPassword);
        return GenericResult<string>.Success(fragment);
    }
}
