using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>A service principal, named <c>SPN</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureAdModes), "Spn")]
public sealed class SpnAzureAdMode : ServicePrincipalAzureAdModeBase
{
    /// <summary>Initializes a new instance of the <see cref="SpnAzureAdMode"/> class.</summary>
    public SpnAzureAdMode()
        : base("Spn", "SPN")
    {
    }
}
