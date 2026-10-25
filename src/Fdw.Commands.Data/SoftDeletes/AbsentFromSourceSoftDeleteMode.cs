using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>Flags the rows whose keys are absent from a source set (rows that disappeared from the source).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SoftDeleteModes), "AbsentFromSource", RestrictToCurrentCompilation = true)]
public sealed class AbsentFromSourceSoftDeleteMode : SoftDeleteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsentFromSourceSoftDeleteMode"/> class.
    /// </summary>
    public AbsentFromSourceSoftDeleteMode()
        : base("AbsentFromSource", "Flag the rows whose keys are absent from the source set", requiresMarker: false)
    {
    }

    /// <inheritdoc />
    public override bool NeedsSourceSet => true;

    /// <inheritdoc />
    public override string? Problem(SoftDeleteOptions options)
    {
        if (options.Marker is not null)
            return "an absent-from-source soft delete takes no marker";
        if (options.AbsentFromSource is null)
            return "an absent-from-source soft delete names the source set";
        return options.AbsentFromSource.KeyColumns.Count == 0
            ? "the absent-key set names at least one key column"
            : null;
    }
}
