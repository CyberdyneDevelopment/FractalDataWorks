using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>A person signs in in a browser.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureAdModes), "Interactive")]
public sealed class InteractiveAzureAdMode : AzureAdModeBase
{
    /// <summary>Initializes a new instance of the <see cref="InteractiveAzureAdMode"/> class.</summary>
    public InteractiveAzureAdMode()
        : base("Interactive", "INTERACTIVE", "Authentication=Active Directory Interactive;")
    {
    }
}
