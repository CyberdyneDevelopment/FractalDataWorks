using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Failed to get columns.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "GetColumnsFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GetColumnsFailedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetColumnsFailedCode"/> class.
    /// </summary>
    public GetColumnsFailedCode()
        : base(71002, "GetColumnsFailed",
            ResultSeverities.ByName("Error"),
            "Failed to get columns: {error}",
            isRetryable: true)
    {
    }
}