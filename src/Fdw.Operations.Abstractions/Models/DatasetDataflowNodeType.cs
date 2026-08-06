using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Container for fields.</summary>
[TypeOption(typeof(DataflowNodeTypes), "Dataset")]
[ExcludeFromCodeCoverage]
public sealed class DatasetDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DatasetDataflowNodeType"/>.</summary>
    public DatasetDataflowNodeType() : base(1, "Dataset") { }
}
