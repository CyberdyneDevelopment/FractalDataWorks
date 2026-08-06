using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet has no sources configured.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataSetNoSources", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetNoSourcesCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetNoSourcesCode"/> class.
    /// </summary>
    public DataSetNoSourcesCode()
        : base(21006, "DataSetNoSources", ResultSeverities.ByName("Error"),
            "DataSet '{DataSetName}' has no sources configured",
            isRetryable: false)
    {
    }
}