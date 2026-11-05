using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A thing was to be placed in an organization's default workspace, but that workspace is not there (FDW-3692).
/// </summary>
[TypeOption(typeof(WorkspacesResultCodes), "DefaultWorkspaceMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DefaultWorkspaceMissingCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultWorkspaceMissingCode"/> class.</summary>
    public DefaultWorkspaceMissingCode()
        : base(20042, "DefaultWorkspaceMissing",
            ResultSeverities.ByName("Error"),
            "'{name}' cannot be placed: organization '{organization}' names a default workspace that has no current configuration ({reason})",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
