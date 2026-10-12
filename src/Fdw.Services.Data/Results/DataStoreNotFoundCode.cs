using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataStore was not found.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataStoreNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreNotFoundCode"/> class.
    /// </summary>
    public DataStoreNotFoundCode()
        : base(31006, "DataStoreNotFound", ResultSeverities.ByName("Error"),
            "DataStore '{DataStoreName}' not found",
            isRetryable: false)
    {
    }
}