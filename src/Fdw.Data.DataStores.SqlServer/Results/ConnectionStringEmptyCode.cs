using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Connection string cannot be null or empty.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "ConnectionStringEmpty", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionStringEmptyCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStringEmptyCode"/> class.
    /// </summary>
    public ConnectionStringEmptyCode()
        : base(21000, "ConnectionStringEmpty",
            ResultSeverities.ByName("Error"),
            "Connection string cannot be null or empty",
            isRetryable: false)
    {
    }
}