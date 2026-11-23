using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The lineage was built from a map edge whose relation no <c>MapRelationOptions</c> option holds (FDW-4404).
/// </summary>
/// <remarks>
/// A fault in the platform's own code, not in the request: the edges are built by the map itself, so a relation with no
/// option means an option is missing. It is told as a server error rather than the edge being left out of the lineage
/// without a word, as the code before this one did. HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "MapRelationUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MapRelationUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="MapRelationUnknownCode"/> class.</summary>
    public MapRelationUnknownCode()
        : base(90011, "MapRelationUnknown",
            ResultSeverities.ByName("Error"),
            "The map has no edge relation '{relation}'",
            isRetryable: false)
    {
    }
}
