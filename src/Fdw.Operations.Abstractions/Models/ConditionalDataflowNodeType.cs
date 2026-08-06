using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Conditional logic.</summary>
[TypeOption(typeof(DataflowNodeTypes), "Conditional")]
[ExcludeFromCodeCoverage]
public sealed class ConditionalDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ConditionalDataflowNodeType"/>.</summary>
    public ConditionalDataflowNodeType() : base(7, "Conditional") { }
}
