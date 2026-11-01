using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Failed to save DataStore.
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "DataStoreSaveFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreSaveFailedCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreSaveFailedCode"/> class.
    /// </summary>
    public DataStoreSaveFailedCode()
        : base(70001, "DataStoreSaveFailed",
            ResultSeverities.ByName("Error"),
            "Failed to save DataStore: {error}",
            isRetryable: true)
    {
    }
}