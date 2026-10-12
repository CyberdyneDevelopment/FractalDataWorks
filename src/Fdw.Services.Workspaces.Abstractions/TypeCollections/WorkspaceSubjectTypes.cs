using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>The kinds of thing that can hold a workspace membership.</summary>
/// <remarks>
/// Closed, and closed for a stronger reason than the lifecycle sets: there is no third kind a
/// future domain could contribute. Something either is a person or is a named group of people.
/// That is why workspace.WorkspaceMember.SubjectType carries a CHECK constraint while
/// WorkspaceResource.ResourceType deliberately does not.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WorkspaceSubjectTypeBase), typeof(IWorkspaceSubjectType), typeof(WorkspaceSubjectTypes))]
public abstract partial class WorkspaceSubjectTypes : TypeCollectionBase<WorkspaceSubjectTypeBase, IWorkspaceSubjectType>
{
}
