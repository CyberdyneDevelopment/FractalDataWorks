using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>
/// Everything upstream of the given DataSet — it is the descendant of every result.
/// </summary>
[TypeOption(typeof(LineageClosureDirections), "Upstream")]
public sealed class UpstreamLineageClosureDirection : LineageClosureDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpstreamLineageClosureDirection"/> class.
    /// </summary>
    public UpstreamLineageClosureDirection()
        : base(2, "Upstream", nameof(DataSetLineageClosureRow.DescendantId))
    {
    }
}
