using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Output field to target.</summary>
[TypeOption(typeof(DataflowNodeTypes), "TargetField")]
[ExcludeFromCodeCoverage]
public sealed class TargetFieldDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="TargetFieldDataflowNodeType"/>.</summary>
    public TargetFieldDataflowNodeType() : base(3, "TargetField") { }
}
