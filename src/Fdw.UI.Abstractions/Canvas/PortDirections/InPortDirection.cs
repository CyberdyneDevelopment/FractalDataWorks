using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

// Why: parent namespace (not a ...Canvas.PortDirections folder-namespace) — a nested namespace
// matching the PortDirections TYPE name collides (CS0101). Lives alongside the collection it joins.
namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// An input port — receives data or control flow from an incoming edge.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PortDirections), "In")]
public sealed class InPortDirection : PortDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InPortDirection"/> class.
    /// </summary>
    public InPortDirection() : base(1, "In")
    {
    }
}
