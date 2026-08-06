using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet was not found in the registry.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataSetNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetNotFoundCode"/> class.
    /// </summary>
    public DataSetNotFoundCode()
        : base(31005, "DataSetNotFound", ResultSeverities.ByName("Error"),
            "DataSet '{DataSetName}' not found",
            isRetryable: false)
    {
    }
}