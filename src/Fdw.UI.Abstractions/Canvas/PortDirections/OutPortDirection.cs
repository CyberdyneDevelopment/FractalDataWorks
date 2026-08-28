using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// An output port — emits data or control flow through an outgoing edge.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PortDirections), "Out")]
public sealed class OutPortDirection : PortDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutPortDirection"/> class.
    /// </summary>
    public OutPortDirection() : base(2, "Out")
    {
    }
}
