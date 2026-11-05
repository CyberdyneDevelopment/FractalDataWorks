using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A provider asked for its members to be placed as a kind that is not a workspace resource kind (FDW-3692).
/// </summary>
[TypeOption(typeof(WorkspacesResultCodes), "PlacementKindUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PlacementKindUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PlacementKindUnknownCode"/> class.</summary>
    public PlacementKindUnknownCode()
        : base(20045, "PlacementKindUnknown",
            ResultSeverities.ByName("Error"),
            "'{name}' cannot be placed: '{kind}' is not a workspace resource kind. The kinds are: {kinds}",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
