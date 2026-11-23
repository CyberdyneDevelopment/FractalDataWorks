using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>The host's managed identity signs in.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureAdModes), "ManagedIdentity")]
public sealed class ManagedIdentityAzureAdMode : AzureAdModeBase
{
    /// <summary>Initializes a new instance of the <see cref="ManagedIdentityAzureAdMode"/> class.</summary>
    public ManagedIdentityAzureAdMode()
        : base("ManagedIdentity", "MANAGEDIDENTITY", "Authentication=Active Directory Managed Identity;")
    {
    }
}
