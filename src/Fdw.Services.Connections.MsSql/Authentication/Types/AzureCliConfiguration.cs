using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.MsSql.Authentication.Types;

/// <summary>
/// Azure CLI token-based authentication. No KVP keys required; SqlClient's own
/// Authentication=ActiveDirectoryDefault pipeline acquires the token.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlAuthenticationTypes), "AzureCli")]
public sealed class AzureCliConfiguration : MsSqlAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="AzureCliConfiguration"/> class.</summary>
    public AzureCliConfiguration()
        : base("AzureCli",
               "Azure CLI",
               "Azure CLI token-based auth (developer machines)",
               [], [], requiresSecret: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success(string.Empty);

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc/>
    /// <remarks>The token comes from the Azure CLI login of the account the host runs as.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => true;
}
