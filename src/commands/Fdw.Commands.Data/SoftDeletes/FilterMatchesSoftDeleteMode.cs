using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>Flags the rows the command's own filter matches: no marker and no source set.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SoftDeleteModes), "FilterMatches", RestrictToCurrentCompilation = true)]
public sealed class FilterMatchesSoftDeleteMode : SoftDeleteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterMatchesSoftDeleteMode"/> class.
    /// </summary>
    public FilterMatchesSoftDeleteMode()
        : base("FilterMatches", "Flag the rows the command's filter matches", requiresMarker: false)
    {
    }

    /// <inheritdoc />
    public override IGenericMessage? Problem(SoftDeleteOptions options)
    {
        if (options.Marker is not null)
            return Problem("a filter-matches soft delete takes no marker");
        return options.AbsentFromSource is not null || options.AbsentFromStage is not null ? Problem("a filter-matches soft delete takes no source set") : null;
    }
}
