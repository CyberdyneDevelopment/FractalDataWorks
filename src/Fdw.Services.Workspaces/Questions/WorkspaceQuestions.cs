using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces;

/// <summary>The questions the workspace access policy answers: see, view, write and read rows.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WorkspaceQuestionBase), typeof(IWorkspaceQuestion), typeof(WorkspaceQuestions))]
public abstract partial class WorkspaceQuestions : TypeCollectionBase<WorkspaceQuestionBase, IWorkspaceQuestion>
{
}
