using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>A way of choosing the rows a soft delete flags.</summary>
public interface ISoftDeleteMode : ITypeOption<int, SoftDeleteModeBase>
{
    /// <summary>
    /// Gets a value indicating whether this mode selects rows by a marker column and value: a marker is required
    /// when true and refused when false.
    /// </summary>
    bool RequiresMarker { get; }

    /// <summary>
    /// Gets a value indicating whether this mode flags the rows whose keys are absent from a source set, which a connection
    /// must stage to compare against: refused by a kind that cannot.
    /// </summary>
    bool NeedsSourceSet { get; }

    /// <summary>Describes the first thing wrong with the options for this mode, or returns <see langword="null"/> when they suit it.</summary>
    /// <param name="options">The options being checked.</param>
    /// <returns>A plain-language reason, or <see langword="null"/>.</returns>
    string? Problem(SoftDeleteOptions options);

    /// <summary>
    /// Gets the column-equals-value conditions this mode adds to the rows the command's own filter selects: a marker mode adds its
    /// marker, the others add none.
    /// </summary>
    /// <param name="options">The options carrying the mode's inputs.</param>
    /// <returns>The conditions, none for a mode that adds none.</returns>
    IReadOnlyList<SoftDeleteMarker> Conditions(SoftDeleteOptions options);
}
