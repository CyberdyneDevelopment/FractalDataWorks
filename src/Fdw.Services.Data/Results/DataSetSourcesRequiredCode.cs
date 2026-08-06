using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet must have at least one source configured.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataSetSourcesRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetSourcesRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetSourcesRequiredCode"/> class.
    /// </summary>
    public DataSetSourcesRequiredCode()
        : base(21007, "DataSetSourcesRequired", ResultSeverities.ByName("Error"),
            "DataSet must have at least one source configured",
            isRetryable: false)
    {
    }
}