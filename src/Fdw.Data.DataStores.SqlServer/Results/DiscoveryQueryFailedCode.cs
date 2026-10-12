using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Discovery query failed.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "DiscoveryQueryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DiscoveryQueryFailedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryQueryFailedCode"/> class.
    /// </summary>
    public DiscoveryQueryFailedCode()
        : base(71001, "DiscoveryQueryFailed",
            ResultSeverities.ByName("Error"),
            "Discovery query failed: {error}",
            isRetryable: true)
    {
    }
}