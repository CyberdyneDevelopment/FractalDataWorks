using Fdw.Collections;

namespace Fdw.Services.Workspaces;

/// <summary>A question the access policy answers about a workspace: see, view, write or read rows.</summary>
public interface IWorkspaceQuestion : ITypeOption<int, WorkspaceQuestionBase>
{
    /// <summary>Gets why the answer was no, for the refusal's details.</summary>
    string Reason { get; }

    /// <summary>Whether the caller's standing answers this question yes.</summary>
    /// <param name="held">The caller's standing in the workspace.</param>
    /// <returns><c>true</c> when the question is granted.</returns>
    bool Grants(WorkspaceStanding held);

    /// <summary>The refusal code to answer with when this question is refused.</summary>
    /// <param name="views">Whether the caller may view the workspace -- whether they have it at all.</param>
    /// <returns>A <c>WorkspacesResultCodes</c> name.</returns>
    /// <remarks>
    /// Viewing is having it (FDW-937): a refusal to a caller who may not view the workspace is a 404, so
    /// no answer confirms a workspace the caller does not have.
    /// </remarks>
    string RefusalCode(bool views);
}
