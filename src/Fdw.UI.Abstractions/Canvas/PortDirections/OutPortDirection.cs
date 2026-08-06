using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

// Why: parent namespace (not a ...Canvas.PortDirections folder-namespace) — a nested namespace
// matching the PortDirections TYPE name collides (CS0101). Lives alongside the collection it joins.
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
