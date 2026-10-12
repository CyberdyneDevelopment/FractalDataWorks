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
}
