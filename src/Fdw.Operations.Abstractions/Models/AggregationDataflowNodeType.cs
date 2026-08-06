using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Aggregation operation.</summary>
[TypeOption(typeof(DataflowNodeTypes), "Aggregation")]
[ExcludeFromCodeCoverage]
public sealed class AggregationDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="AggregationDataflowNodeType"/>.</summary>
    public AggregationDataflowNodeType() : base(6, "Aggregation") { }
}
