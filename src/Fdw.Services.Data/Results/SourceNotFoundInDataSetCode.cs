using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Source was not found in the DataSet.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "SourceNotFoundInDataSet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceNotFoundInDataSetCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceNotFoundInDataSetCode"/> class.
    /// </summary>
    public SourceNotFoundInDataSetCode()
        : base(31011, "SourceNotFoundInDataSet", ResultSeverities.ByName("Error"),
            "Source '{SourceName}' not found in DataSet '{DataSetName}'",
            isRetryable: false)
    {
    }
}