using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A thing was to be placed, but the caller is not a user whose id can be recorded (FDW-3692).
/// </summary>
[TypeOption(typeof(WorkspacesResultCodes), "PlacementUserUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PlacementUserUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PlacementUserUnknownCode"/> class.</summary>
    public PlacementUserUnknownCode()
        : base(20044, "PlacementUserUnknown",
            ResultSeverities.ByName("Error"),
            "'{name}' cannot be placed: the caller could not be resolved to a user id to record as the one who placed it",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
