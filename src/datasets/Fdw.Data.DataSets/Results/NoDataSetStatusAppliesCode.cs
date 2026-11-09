using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// No option of <c>DataSetStatuses</c> applies to a data set (FDW-4488).
/// </summary>
/// <remarks>
/// The shipped options cover every data set (Unwritten has no writers, Idle has some), so this is a fault in the collection a host configured: a developer removed or
/// replaced the options without leaving one that claims the case. It is an internal-category code, and the list stops instead of showing a status nobody chose.
/// </remarks>
[TypeOption(typeof(DataSetsResultCodes), "NoDataSetStatusApplies", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoDataSetStatusAppliesCode : DataSetsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="NoDataSetStatusAppliesCode"/> class.</summary>
    public NoDataSetStatusAppliesCode()
        : base(90002, "NoDataSetStatusApplies",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' has no status: no option of DataSetStatuses applies to its {writerCount} writer(s), so the options configured for this host leave the case unclaimed",
            isRetryable: false)
    {
    }
}
