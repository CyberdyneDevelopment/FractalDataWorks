using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// The Entra ID connection names an AzureAdMode SqlClient does not take (Default, ServicePrincipal or SPN, Interactive, ManagedIdentity or MSI), so the connection cannot be built.
/// </summary>
[TypeOption(typeof(MsSqlConnectionResultCodes), "AzureAdModeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AzureAdModeUnknownCode : MsSqlConnectionResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdModeUnknownCode"/> class.
    /// </summary>
    public AzureAdModeUnknownCode()
        : base(
            60015,
            "AzureAdModeUnknown",
            ResultSeverities.ByName("Error"),
            "The Entra ID connection names an AzureAdMode SqlClient does not take (Default, ServicePrincipal or SPN, Interactive, ManagedIdentity or MSI), so the connection cannot be built.")
    {
    }
}
