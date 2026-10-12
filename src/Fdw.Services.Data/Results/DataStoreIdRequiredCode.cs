using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataStore Id was null or empty.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataStoreIdRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreIdRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreIdRequiredCode"/> class.
    /// </summary>
    public DataStoreIdRequiredCode()
        : base(21008, "DataStoreIdRequired", ResultSeverities.ByName("Error"),
            "DataStore Id cannot be null or empty",
            isRetryable: false)
    {
    }
}