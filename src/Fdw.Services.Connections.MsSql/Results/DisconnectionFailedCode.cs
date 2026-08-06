using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Disconnection from SQL Server failed.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "DisconnectionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DisconnectionFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisconnectionFailedCode"/> class.
    /// </summary>
    public DisconnectionFailedCode()
        : base(
            71001,
            "DisconnectionFailed",
            ResultSeverities.ByName("Warning"),
            "Failed to disconnect from SQL Server: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
