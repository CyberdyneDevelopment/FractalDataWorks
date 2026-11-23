using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Workspaces.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// One container of a shared data store, owned by a workspace (FDW-6967).
/// </summary>
/// <remarks>
/// Declared here, beside <see cref="DataStoreResourceKind"/>, because this package owns the data store and its containers.
/// The store stays the platform's (<see cref="DataStoreResourceKind"/> cannot be owned); a workspace owns one container of
/// it, and owning it takes nothing from the store's other users. The owner is one <c>Owns</c> row of
/// <c>workspace.WorkspaceResource</c> naming the container's Id, as for every other thing a workspace owns.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceKinds), "DataContainer")]
public sealed class DataContainerResourceKind : WorkspaceResourceKindBase
{
    /// <summary>The kind's name, which <c>workspace.WorkspaceResource.ResourceType</c> stores.</summary>
    public const string KindName = "DataContainer";

    /// <summary>Initializes a new instance of the <see cref="DataContainerResourceKind"/> class.</summary>
    /// <remarks>
    /// Why <c>releasedOnArchive: true</c>: the container is the store's, lent to the workspace to own. A closed workspace cannot be
    /// the scope its writes are decided at, so archiving the workspace gives the container back to the store (FDW-6967).
    /// </remarks>
    public DataContainerResourceKind()
        : base(KindName, canBeOwned: true, releasedOnArchive: true)
    {
    }
}
