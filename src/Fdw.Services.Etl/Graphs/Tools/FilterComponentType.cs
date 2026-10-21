using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The Filter tool: the Filter stage kind placed in a graph.</summary>
[TypeOption(typeof(ComponentTypes), "Filter")]
public sealed class FilterComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FilterComponentType"/> class.</summary>
    public FilterComponentType() : base(new FilterTransformType(), "Filter", "The rows that meet the condition; the rows that do not are dropped.")
    {
    }
}
