using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataStore name was null or empty.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataStoreNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataStoreNameRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreNameRequiredCode"/> class.
    /// </summary>
    public DataStoreNameRequiredCode()
        : base(21010, "DataStoreNameRequired", ResultSeverities.ByName("Error"),
            "DataStore name cannot be null or empty",
            isRetryable: false)
    {
    }
}