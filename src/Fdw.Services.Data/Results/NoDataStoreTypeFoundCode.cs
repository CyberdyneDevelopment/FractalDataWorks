using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// No DataStoreType found for the specified store type.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "NoDataStoreTypeFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoDataStoreTypeFoundCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoDataStoreTypeFoundCode"/> class.
    /// </summary>
    public NoDataStoreTypeFoundCode()
        : base(31009, "NoDataStoreTypeFound", ResultSeverities.ByName("Error"),
            "No DataStoreType found for store type '{StoreType}'",
            isRetryable: false)
    {
    }
}