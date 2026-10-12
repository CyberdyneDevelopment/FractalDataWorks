using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Workspaces.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// A data set attached to a workspace.
/// </summary>
/// <remarks>
/// Declared here rather than in the workspaces package because this package owns the data set. A
/// host that has not referenced data sets cannot attach one to a workspace, and the resource-kind
/// collection is therefore an accurate description of what that host can actually do.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceKinds), "DataSet")]
public sealed class DataSetResourceKind : WorkspaceResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetResourceKind"/> class.</summary>
    public DataSetResourceKind()
        : base("DataSet", canBeOwned: true)
    {
    }
}
