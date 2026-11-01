using Fdw.Collections;

namespace Fdw.Services.Workspaces;

/// <summary>Base for a question the workspace access policy answers.</summary>
/// <remarks>
/// Each question carries its own rule, its own refusal code and its own reason, so the policy asks the
/// question rather than switching on which one it is, and a question added later arrives with its own
/// answer.
/// </remarks>
public abstract class WorkspaceQuestionBase : TypeOptionBase<WorkspaceQuestionBase>, IWorkspaceQuestion
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceQuestionBase"/> class.</summary>
    /// <param name="name">The question's name.</param>
    /// <param name="reason">Why the answer was no, for the refusal's details.</param>
    protected WorkspaceQuestionBase(string name, string reason) : base(name)
    {
        Reason = reason;
    }

    /// <inheritdoc />
    public string Reason { get; }

    /// <inheritdoc />
    public abstract bool Grants(WorkspaceStanding held);

    /// <inheritdoc />
    public abstract string RefusalCode(bool views);
}
