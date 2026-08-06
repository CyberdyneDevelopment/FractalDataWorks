using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Data transformation.</summary>
[TypeOption(typeof(DataflowNodeTypes), "Transform")]
[ExcludeFromCodeCoverage]
public sealed class TransformDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="TransformDataflowNodeType"/>.</summary>
    public TransformDataflowNodeType() : base(9, "Transform") { }
}
