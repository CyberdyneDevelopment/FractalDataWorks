using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataStore Location was null or empty.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataStoreLocationRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreLocationRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreLocationRequiredCode"/> class.
    /// </summary>
    public DataStoreLocationRequiredCode()
        : base(21009, "DataStoreLocationRequired", ResultSeverities.ByName("Error"),
            "DataStore Location cannot be null or empty",
            isRetryable: false)
    {
    }
}