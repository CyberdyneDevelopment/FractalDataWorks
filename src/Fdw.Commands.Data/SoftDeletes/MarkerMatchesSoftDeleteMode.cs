using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>Flags the rows whose marker column equals the marker value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SoftDeleteModes), "MarkerMatches", RestrictToCurrentCompilation = true)]
public sealed class MarkerMatchesSoftDeleteMode : SoftDeleteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkerMatchesSoftDeleteMode"/> class.
    /// </summary>
    public MarkerMatchesSoftDeleteMode()
        : base("MarkerMatches", "Flag the rows whose marker column equals the marker value", requiresMarker: true)
    {
    }
}
