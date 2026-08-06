using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataStore was null.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataStoreRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreRequiredCode"/> class.
    /// </summary>
    public DataStoreRequiredCode()
        : base(21011, "DataStoreRequired", ResultSeverities.ByName("Error"),
            "DataStore cannot be null",
            isRetryable: false)
    {
    }
}