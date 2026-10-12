using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A membership named a user or role that does not resolve for the caller.
/// </summary>
/// <remarks>
/// Caller-input validation, HTTP 400. A member row for a subject that does not exist, or that the caller
/// cannot see, would grant nothing to anyone and hide the mistake (FDW-1071). The message is shown to the caller.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceMemberSubjectUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceMemberSubjectUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceMemberSubjectUnknownCode"/> class.</summary>
    public WorkspaceMemberSubjectUnknownCode()
        : base(20008, "WorkspaceMemberSubjectUnknown",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' member refused: {subjectType} '{subjectId}' does not resolve",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
