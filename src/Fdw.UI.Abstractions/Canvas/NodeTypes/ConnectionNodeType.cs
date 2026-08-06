using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A configured data connection node (e.g. SQL Server, HTTP endpoint).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "Connection")]
public sealed class ConnectionNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionNodeType"/> class.
    /// </summary>
    public ConnectionNodeType()
        : base(1, "Connection", "Connection", "Infrastructure", "plug")
    {
    }
}
