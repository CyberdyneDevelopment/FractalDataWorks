using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The map was asked to count or name a kind of object that no <c>MapKindOptions</c> option holds (FDW-4404).
/// </summary>
/// <remarks>
/// A fault in the platform's own code, not in the request: the kinds the summary counts are declared by the map itself,
/// so one with no option means an option is missing. It is told as a server error rather than answered with another kind's
/// provider, as the code before this one did. HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "MapKindUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MapKindUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="MapKindUnknownCode"/> class.</summary>
    public MapKindUnknownCode()
        : base(90010, "MapKindUnknown",
            ResultSeverities.ByName("Error"),
            "The map has no kind '{kind}'",
            isRetryable: false)
    {
    }
}
