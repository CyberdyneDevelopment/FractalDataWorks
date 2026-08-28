using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

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
