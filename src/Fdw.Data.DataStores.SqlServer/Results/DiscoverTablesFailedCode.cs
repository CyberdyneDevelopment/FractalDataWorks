using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Failed to discover tables.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "DiscoverTablesFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DiscoverTablesFailedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverTablesFailedCode"/> class.
    /// </summary>
    public DiscoverTablesFailedCode()
        : base(71000, "DiscoverTablesFailed",
            ResultSeverities.ByName("Error"),
            "Failed to discover tables: {error}",
            isRetryable: true)
    {
    }
}