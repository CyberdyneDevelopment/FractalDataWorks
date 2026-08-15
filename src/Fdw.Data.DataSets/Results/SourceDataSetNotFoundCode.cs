using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The source DataSet named as a DataSet source's target was not found.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceDataSetNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceDataSetNotFoundCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDataSetNotFoundCode"/> class.
    /// </summary>
    public SourceDataSetNotFoundCode()
        : base(31100, "SourceDataSetNotFound", ResultSeverities.ByName("Error"),
            "Source DataSet '{SourceDataSetName}' referenced by DataSet '{DataSetName}' was not found",
            isRetryable: false)
    {
    }
}
