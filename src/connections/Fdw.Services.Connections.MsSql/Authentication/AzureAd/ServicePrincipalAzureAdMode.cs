using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>A service principal, named <c>SERVICEPRINCIPAL</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureAdModes), "ServicePrincipal")]
public sealed class ServicePrincipalAzureAdMode : ServicePrincipalAzureAdModeBase
{
    /// <summary>Initializes a new instance of the <see cref="ServicePrincipalAzureAdMode"/> class.</summary>
    public ServicePrincipalAzureAdMode()
        : base("ServicePrincipal", "SERVICEPRINCIPAL")
    {
    }
}
