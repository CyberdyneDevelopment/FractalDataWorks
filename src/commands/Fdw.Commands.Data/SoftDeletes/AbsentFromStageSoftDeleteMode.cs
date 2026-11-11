using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Messages;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// Flags the rows whose keys have no row in a stage the load filled batch by batch (FDW-4956): the end-of-run sweep of an <c>AbsentFromSource</c> load,
/// which never holds every row. It is built by the load, never stated by one.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SoftDeleteModes), "AbsentFromStage", RestrictToCurrentCompilation = true)]
public sealed class AbsentFromStageSoftDeleteMode : SoftDeleteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsentFromStageSoftDeleteMode"/> class.
    /// </summary>
    public AbsentFromStageSoftDeleteMode()
        : base("AbsentFromStage", "Flag the rows whose keys have no row in the load's stage", requiresMarker: false)
    {
    }

    /// <inheritdoc />
    public override bool NeedsSourceSet => true;

    /// <inheritdoc />
    /// <remarks>
    /// Why an expected count of at least one: a stage that is empty, or holds fewer rows than were appended, would make every target row look gone. The
    /// translator compares the count with the stage in the same batch as the sweep.
    /// </remarks>
    public override IGenericMessage? Problem(SoftDeleteOptions options)
    {
        if (options.Marker is not null)
            return Problem("an absent-from-stage soft delete takes no marker");
        if (options.AbsentFromSource is not null)
            return Problem("an absent-from-stage soft delete takes a stage, not a source set");
        if (options.AbsentFromStage is not { } staged)
            return Problem("an absent-from-stage soft delete names its stage");
        if (staged.Stage is null)
            return Problem("an absent-from-stage soft delete names the stage container");
        if (string.IsNullOrWhiteSpace(staged.StageKeyColumn))
            return Problem("an absent-from-stage soft delete names the stage key column");
        if (string.IsNullOrWhiteSpace(staged.StageKey))
            return Problem("an absent-from-stage soft delete states its stage key");
        if (staged.KeyColumns.Count == 0 || staged.KeyColumns.Any(string.IsNullOrWhiteSpace))
            return Problem("the staged key set names at least one key column, none blank");
        return staged.ExpectedRows < 1
            ? Problem("an absent-from-stage soft delete states the rows it staged, at least one")
            : null;
    }
}
