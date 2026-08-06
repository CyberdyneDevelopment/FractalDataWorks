using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// SQL Server connection failed (errors -1, 2, 53). The database server is unreachable.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "SqlConnectionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlConnectionFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlConnectionFailedCode"/> class.
    /// </summary>
    public SqlConnectionFailedCode()
        : base(
            71002,
            "SqlConnectionFailed",
            ResultSeverities.ByName("Error"),
            "Cannot connect to SQL Server at '{ServerAddress}'. Verify the server is running and the network is reachable.",
            isRetryable: true)
    {
    }
}
