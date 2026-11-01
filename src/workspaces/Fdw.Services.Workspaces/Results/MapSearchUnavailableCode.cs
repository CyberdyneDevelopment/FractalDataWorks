using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A lineage search cannot be answered from a domain table because the provider registered for a kind cannot list its
/// domain rows (FDW-1195).
/// </summary>
/// <remarks>
/// A wiring fault, not a request fault: the data set, data store and pipeline providers the platform registers all list
/// their domain table, so this is told as a server error rather than answered with a slow whole-map read in its place.
/// HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "MapSearchUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MapSearchUnavailableCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="MapSearchUnavailableCode"/> class.</summary>
    public MapSearchUnavailableCode()
        : base(90007, "MapSearchUnavailable",
            ResultSeverities.ByName("Error"),
            "Lineage search is unavailable: the {kind} provider cannot list its domain table",
            isRetryable: false)
    {
    }
}
