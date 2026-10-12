using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>
/// The kinds of resource that can be attached to a workspace.
/// </summary>
/// <remarks>
/// <para>
/// This collection gathers options that attach themselves. The package owning a resource declares
/// its own kind — <c>Fdw.Data.DataSets</c> declares the data set kind, the pipelines package will
/// declare its own — so the set a host understands is exactly the set of domains it has
/// referenced. A host with no pipeline packages cannot attach a pipeline to a workspace, which is
/// correct rather than unfortunate.
/// </para>
/// <para>
/// This is why <c>workspace.WorkspaceResource.ResourceType</c> carries no CHECK constraint. A closed
/// database constraint would have to be widened by a schema change every time a domain was added,
/// and would encode a set the database has no way to know.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WorkspaceResourceKindBase), typeof(IWorkspaceResourceKind), typeof(WorkspaceResourceKinds))]
public abstract partial class WorkspaceResourceKinds
    : TypeCollectionBase<WorkspaceResourceKindBase, IWorkspaceResourceKind>
{
    /// <summary>Gets the service category this collection belongs to.</summary>
    public static string ServiceCategory => "Workspace";
}
