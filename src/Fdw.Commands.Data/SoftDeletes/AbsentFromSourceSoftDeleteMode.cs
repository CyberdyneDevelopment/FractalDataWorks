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
}
