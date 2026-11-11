using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;

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
    public override IGenericMessage? Problem(SoftDeleteOptions options)
    {
        if (options.Marker is not null)
            return Problem("an absent-from-source soft delete takes no marker");
        if (options.AbsentFromStage is not null)
            return Problem("an absent-from-source soft delete takes a source set, not a stage");
        if (options.AbsentFromSource is null)
            return Problem("an absent-from-source soft delete names the source set");
        return options.AbsentFromSource.KeyColumns.Count == 0
            ? Problem("the absent-key set names at least one key column")
            : null;
    }
}
