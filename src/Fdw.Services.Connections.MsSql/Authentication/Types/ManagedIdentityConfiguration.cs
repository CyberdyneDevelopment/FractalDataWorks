using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;

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
        : base(4, "ManagedIdentity",
               "Managed Identity",
               "Authenticate using Azure Managed Identity",
               [], [], [])
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values) => GenericResult.Success();

    /// <inheritdoc/>
    public override IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword)
        => GenericResult<string>.Success("Authentication=Active Directory Default;");
}
