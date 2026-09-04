using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>
/// Everything downstream of the given DataSet — it is the ancestor of every result.
/// </summary>
[TypeOption(typeof(LineageClosureDirections), "Downstream")]
public sealed class DownstreamLineageClosureDirection : LineageClosureDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DownstreamLineageClosureDirection"/> class.
    /// </summary>
    public DownstreamLineageClosureDirection()
        : base(1, "Downstream", nameof(DataSetLineageClosureRow.AncestorId))
    {
    }
}
