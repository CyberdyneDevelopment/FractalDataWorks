using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace was to be made in a tenant's default organization, but the tenant has none (FDW-3668).
/// </summary>
[TypeOption(typeof(WorkspacesResultCodes), "DefaultOrganizationMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DefaultOrganizationMissingCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultOrganizationMissingCode"/> class.</summary>
    public DefaultOrganizationMissingCode()
        : base(20041, "DefaultOrganizationMissing",
            ResultSeverities.ByName("Error"),
            "'{name}' names no organization and cannot be placed in its tenant's default organization: {reason}",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
