using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataStore StoreType was null or empty.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataStoreTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreTypeRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreTypeRequiredCode"/> class.
    /// </summary>
    public DataStoreTypeRequiredCode()
        : base(21012, "DataStoreTypeRequired", ResultSeverities.ByName("Error"),
            "DataStore StoreType cannot be null or empty",
            isRetryable: false)
    {
    }
}