using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Cannot discover the specified DataStore.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "CannotDiscoverDataStore", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CannotDiscoverDataStoreCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CannotDiscoverDataStoreCode"/> class.
    /// </summary>
    public CannotDiscoverDataStoreCode()
        : base(30000, "CannotDiscoverDataStore", ResultSeverities.ByName("Error"),
            "Cannot discover DataStore '{DataStoreName}': {Reason}",
            isRetryable: false)
    {
    }
}