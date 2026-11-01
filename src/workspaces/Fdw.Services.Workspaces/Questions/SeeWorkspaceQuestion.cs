using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces;

/// <summary>Whether the caller can find the workspace to ask to join it.</summary>
/// <remarks>
/// Only the membership request asks this. Listings ask <see cref="ViewWorkspaceQuestion"/>: a
/// workspace is listed for exactly the callers who may view it (FDW-937).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceQuestions), "See")]
public sealed class SeeWorkspaceQuestion : WorkspaceQuestionBase
{
    /// <summary>Initializes a new instance of the <see cref="SeeWorkspaceQuestion"/> class.</summary>
    public SeeWorkspaceQuestion()
        : base("See", "it is not listed, and the caller neither owns it, is a member of it, nor may view it")
    {
    }

    /// <inheritdoc />
    /// <remarks>Viewing it, any membership at all, or a visibility that lists it.</remarks>
    public override bool Grants(WorkspaceStanding held)
        => WorkspaceQuestions.View.Grants(held) || held.Roles.Any(role => role.MayWrite) || held.Visibility.IsListed;

    /// <inheritdoc />
    /// <remarks>Always a 404: a refusal never confirms a workspace the caller does not have.</remarks>
    public override string RefusalCode(bool views) => "WorkspaceNotVisible";
}
