using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// ConnectionId cannot be empty.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "ConnectionIdEmpty", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionIdEmptyCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionIdEmptyCode"/> class.
    /// </summary>
    public ConnectionIdEmptyCode()
        : base(20000, "ConnectionIdEmpty",
            ResultSeverities.ByName("Error"),
            "ConnectionId cannot be empty",
            isRetryable: false)
    {
    }
}