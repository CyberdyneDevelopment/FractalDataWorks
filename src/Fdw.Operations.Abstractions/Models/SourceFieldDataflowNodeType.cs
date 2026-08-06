using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Input field from source.</summary>
[TypeOption(typeof(DataflowNodeTypes), "SourceField")]
[ExcludeFromCodeCoverage]
public sealed class SourceFieldDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="SourceFieldDataflowNodeType"/>.</summary>
    public SourceFieldDataflowNodeType() : base(2, "SourceField") { }
}
