using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Connection to SQL Server failed.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "ConnectionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedCode"/> class.
    /// </summary>
    public ConnectionFailedCode()
        : base(
            70000,
            "ConnectionFailed",
            ResultSeverities.ByName("Error"),
            "Failed to connect to SQL Server: {ErrorMessage}",
            isRetryable: true)
    {
    }
}
