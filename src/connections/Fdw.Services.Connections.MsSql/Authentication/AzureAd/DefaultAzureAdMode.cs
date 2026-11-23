using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.MsSql.Authentication.Types;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>SqlClient's own default mode, which a connection that names none asks for.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureAdModes), "Default")]
public sealed class DefaultAzureAdMode : AzureAdModeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultAzureAdMode"/> class.</summary>
    public DefaultAzureAdMode()
        : base("Default", EntraIdConfiguration.SqlClientDefaultMode, "Authentication=Active Directory Default;")
    {
    }
}
