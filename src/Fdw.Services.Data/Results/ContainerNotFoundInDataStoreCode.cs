using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Container was not found in the specified DataStore.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerNotFoundInDataStore", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNotFoundInDataStoreCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNotFoundInDataStoreCode"/> class.
    /// </summary>
    public ContainerNotFoundInDataStoreCode()
        : base(31001, "ContainerNotFoundInDataStore", ResultSeverities.ByName("Error"),
            "Container '{ContainerPath}' not found in DataStore '{DataStoreName}'",
            isRetryable: false)
    {
    }
}