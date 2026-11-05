using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A thing was to be placed in its owner's default workspace, but the caller acts in no tenant (FDW-3692).
/// </summary>
[TypeOption(typeof(WorkspacesResultCodes), "PlacementTenantUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PlacementTenantUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PlacementTenantUnknownCode"/> class.</summary>
    public PlacementTenantUnknownCode()
        : base(20043, "PlacementTenantUnknown",
            ResultSeverities.ByName("Error"),
            "'{name}' cannot be placed: the caller is acting in no tenant, so there is no default organization to place it in",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
