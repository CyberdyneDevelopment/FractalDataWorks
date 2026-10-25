using System.Collections.Generic;
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

    /// <inheritdoc />
    public override string? Problem(SoftDeleteOptions options)
    {
        if (options.AbsentFromSource is not null)
            return "a marker-matches soft delete takes no source set";
        return options.Marker is null || string.IsNullOrWhiteSpace(options.Marker.Column) || options.Marker.Value is null
            ? "a marker-matches soft delete names a marker column and a marker value"
            : null;
    }

    /// <inheritdoc />
    public override IReadOnlyList<SoftDeleteMarker> Conditions(SoftDeleteOptions options)
        => options.Marker is { } marker ? [marker] : [];
}
