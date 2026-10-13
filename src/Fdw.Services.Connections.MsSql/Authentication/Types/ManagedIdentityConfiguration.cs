using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.MsSql.Authentication.Types;

/// <summary>
/// Azure Managed Identity Authentication (Active Directory Default).
/// No KVP keys required.
/// </summary>
[TypeOption(typeof(MsSqlAuthenticationTypes), "ManagedIdentity")]
public sealed class ManagedIdentityConfiguration : MsSqlAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="ManagedIdentityConfiguration"/> class.</summary>
    public ManagedIdentityConfiguration()
        : base("ManagedIdentity",
               "Managed Identity",
               "Authenticate using Azure Managed Identity",
               [], [], requiresSecret: false)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success("Authentication=Active Directory Default;");

    /// <inheritdoc />
    /// <remarks>Carries no stored credential.</remarks>
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [];

    /// <inheritdoc/>
    /// <remarks>The managed identity is the one assigned to the host, not to the connection.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => true;
}
