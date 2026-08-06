using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Nested calculation workflow.</summary>
[TypeOption(typeof(DataflowNodeTypes), "CompoundChain")]
[ExcludeFromCodeCoverage]
public sealed class CompoundChainDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CompoundChainDataflowNodeType"/>.</summary>
    public CompoundChainDataflowNodeType() : base(5, "CompoundChain") { }
}
