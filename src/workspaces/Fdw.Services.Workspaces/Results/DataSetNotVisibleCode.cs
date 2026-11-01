using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A data set named on a workspace route does not exist, or the caller may not see it.
/// </summary>
/// <remarks>
/// One answer for both, like <see cref="WorkspaceNotReadableCode"/>: telling them apart would reveal
/// which data sets exist outside the caller's reach (FDW-937). HTTP 404.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "DataSetNotVisible", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetNotVisibleCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetNotVisibleCode"/> class.</summary>
    public DataSetNotVisibleCode()
        : base(30004, "DataSetNotVisible",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' was not found",
            isRetryable: false)
    {
    }
}
