using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>
/// A data view attached to a workspace.
/// </summary>
/// <remarks>
/// Declared here because the workspaces domain owns the data view type itself.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceKinds), "DataView")]
public sealed class DataViewResourceKind : WorkspaceResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataViewResourceKind"/> class.</summary>
    public DataViewResourceKind()
        : base("DataView", canBeOwned: true)
    {
    }
}
