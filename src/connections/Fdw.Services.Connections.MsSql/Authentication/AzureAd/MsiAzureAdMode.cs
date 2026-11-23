using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>MSI is the short word for ManagedIdentity.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureAdModes), "Msi")]
public sealed class MsiAzureAdMode : AzureAdModeBase
{
    /// <summary>Initializes a new instance of the <see cref="MsiAzureAdMode"/> class.</summary>
    public MsiAzureAdMode()
        : base("Msi", "MSI", "Authentication=Active Directory Managed Identity;")
    {
    }
}
