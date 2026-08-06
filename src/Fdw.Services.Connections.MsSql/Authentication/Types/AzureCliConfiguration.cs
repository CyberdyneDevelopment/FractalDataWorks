using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Azure.Core;
using Azure.Identity;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Authentication.Types;

/// <summary>
/// Azure CLI token-based authentication. No KVP keys required;
/// the access token is acquired via <see cref="AzureCliCredential"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlAuthenticationTypes), "AzureCli")]
public sealed class AzureCliConfiguration : MsSqlAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="AzureCliConfiguration"/> class.</summary>
    public AzureCliConfiguration()
        : base(5, "AzureCli",
               "Azure CLI",
               "Azure CLI token-based auth (developer machines)",
               [], [], [])
    {
    }

    /// <inheritdoc/>
    public override bool UsesAccessToken => true;

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success(string.Empty);

    /// <inheritdoc/>
    public override string? AcquireAccessToken()
    {
        var credential = new AzureCliCredential();
        var token = credential.GetToken(
            new TokenRequestContext(["https://database.windows.net/.default"]),
            CancellationToken.None);
        return token.Token;
    }
}
