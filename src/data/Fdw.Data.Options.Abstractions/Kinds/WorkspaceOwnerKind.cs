using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>A workspace owns the rule. (FDW-6454).</summary>
[TypeOption(typeof(DataOptionOwnerKinds), "Workspace")]
public sealed class WorkspaceOwnerKind : DataOptionOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceOwnerKind"/> class.</summary>
    public WorkspaceOwnerKind()
        : base("Workspace")
    {
    }
}
