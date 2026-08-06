using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Constant value injection.</summary>
[TypeOption(typeof(DataflowNodeTypes), "Constant")]
[ExcludeFromCodeCoverage]
public sealed class ConstantDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ConstantDataflowNodeType"/>.</summary>
    public ConstantDataflowNodeType() : base(8, "Constant") { }
}
