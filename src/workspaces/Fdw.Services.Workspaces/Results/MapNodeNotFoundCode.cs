using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A map node was asked to explode, but the object it stands for no longer exists or is not visible to the caller (FDW-1029).
/// </summary>
/// <remarks>
/// A node id is the kind of the object and its logical id. The map the caller holds can be older than the
/// configuration: the object may have been deleted since it was drawn. Answered as not found, never as an empty
/// set of children, so a client can tell a vanished node from one that simply has nothing under it. HTTP 404.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "MapNodeNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MapNodeNotFoundCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="MapNodeNotFoundCode"/> class.</summary>
    public MapNodeNotFoundCode()
        : base(30005, "MapNodeNotFound",
            ResultSeverities.ByName("Error"),
            "Map node '{node}' was not found",
            isRetryable: false)
    {
    }
}
