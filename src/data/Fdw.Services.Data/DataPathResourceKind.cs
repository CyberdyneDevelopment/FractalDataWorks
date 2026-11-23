using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Workspaces.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// One path of a shared data store, owned by a workspace (FDW-6967).
/// </summary>
/// <remarks>
/// Declared here, beside <see cref="DataStoreResourceKind"/>, because this package owns the data store and its paths.
/// The store stays the platform's (<see cref="DataStoreResourceKind"/> cannot be owned); a workspace owns one path of
/// it, and owning it takes nothing from the store's other users. The owner is one <c>Owns</c> row of
/// <c>workspace.WorkspaceResource</c> naming the path's Id, as for every other thing a workspace owns.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceKinds), "DataPath")]
public sealed class DataPathResourceKind : WorkspaceResourceKindBase
{
    /// <summary>The kind's name, which <c>workspace.WorkspaceResource.ResourceType</c> stores.</summary>
    public const string KindName = "DataPath";

    /// <summary>Initializes a new instance of the <see cref="DataPathResourceKind"/> class.</summary>
    /// <remarks>
    /// Why <c>releasedOnArchive: true</c>: the path is the store's, lent to the workspace to own. A closed workspace cannot be
    /// the scope its writes are decided at, so archiving the workspace gives the path back to the store (FDW-6967).
    /// </remarks>
    public DataPathResourceKind()
        : base(KindName, canBeOwned: true, releasedOnArchive: true)
    {
    }
}
