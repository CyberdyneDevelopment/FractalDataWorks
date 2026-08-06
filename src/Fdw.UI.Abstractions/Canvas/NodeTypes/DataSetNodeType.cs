using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A named dataset schema node representing a logical set of fields.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "DataSet")]
public sealed class DataSetNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetNodeType"/> class.
    /// </summary>
    public DataSetNodeType()
        : base(3, "DataSet", "Data Set", "Data", "table")
    {
    }
}
