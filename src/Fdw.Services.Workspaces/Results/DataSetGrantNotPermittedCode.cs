using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The caller may change the workspace and can see the data set, but may not grant access to it.
/// </summary>
/// <remarks>
/// A workspace serves its data sets past row-level security, so attaching one hands its rows to every
/// member (FDW-937). That takes a grant the caller holds for the data set: <c>datasets:grant</c> for
/// the tenant, or <c>datasets/{id}:grant</c> for the one set. HTTP 403: the caller may already see
/// both, so the refusal can say what it is.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "DataSetGrantNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetGrantNotPermittedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetGrantNotPermittedCode"/> class.</summary>
    public DataSetGrantNotPermittedCode()
        : base(100004, "DataSetGrantNotPermitted",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' cannot be attached to workspace '{name}' by this caller: {reason}",
            isRetryable: false)
    {
    }
}
