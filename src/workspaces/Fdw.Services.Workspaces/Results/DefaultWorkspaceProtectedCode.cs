using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace is the default workspace of its organization, so it cannot be deleted (FDW-3668).
/// </summary>
[TypeOption(typeof(WorkspacesResultCodes), "DefaultWorkspaceProtected", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DefaultWorkspaceProtectedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultWorkspaceProtectedCode"/> class.</summary>
    public DefaultWorkspaceProtectedCode()
        : base(20040, "DefaultWorkspaceProtected",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' is the default workspace of organization '{organization}', where its things are placed when they name no workspace; it cannot be deleted",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
