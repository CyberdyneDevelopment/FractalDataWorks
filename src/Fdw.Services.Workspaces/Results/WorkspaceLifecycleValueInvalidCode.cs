using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace's Status, Visibility or JoinPolicy was missing or not a registered option.
/// </summary>
/// <remarks>
/// Caller-input validation — HTTP 400. Names the field and the offending value, because the
/// alternative is a database CHECK violation that names a constraint and leaves the caller to
/// work out which of three columns it meant. The message is shown to the caller (FDW-1053).
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceLifecycleValueInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceLifecycleValueInvalidCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceLifecycleValueInvalidCode"/> class.</summary>
    public WorkspaceLifecycleValueInvalidCode()
        : base(20001, "WorkspaceLifecycleValueInvalid",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' rejected: {field} '{value}' is not a registered option",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller which field and value were refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
