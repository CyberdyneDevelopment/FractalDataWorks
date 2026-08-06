using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// Computes where a node's ports sit and how tall its body must be to contain them.
/// </summary>
/// <remarks>
/// <para>
/// Ports are laid out as two vertical columns pinned to the node's left (In) and right (Out)
/// edges, evenly spaced and centred on the node. The body grows vertically to contain the taller
/// column, so a Map transform carrying one port per DataSet field renders as a tall mapping panel
/// while an ordinary two-port node keeps its original 40-unit height.
/// </para>
/// <para>
/// Geometry lives in this Blazor renderer package rather than in the render-agnostic
/// <c>Fdw.UI.Abstractions</c> contract, for the same reason <see cref="SvgNodeAppearanceMap"/>
/// does: SVG units and shapes are a rendering concern, not part of the canvas contract.
/// </para>
/// </remarks>
internal static class SvgPortGeometry
{
    /// <summary>
    /// Horizontal distance from a node's centre at which a port-less edge anchors. Sits slightly
    /// outside the widest body so an arrowhead does not overlap the shape's stroke.
    /// </summary>
    public const double NodeAnchorOffset = 70.0;

    /// <summary>Half-height of a node body that has at most one port per column — the renderer's original 40-unit box.</summary>
    public const double DefaultBodyHalfHeight = 20.0;

    /// <summary>Vertical distance between two adjacent ports in the same column.</summary>
    public const double PortSpacing = 16.0;

    /// <summary>Radius of the circle drawn for each port.</summary>
    public const double PortRadius = 4.0;

    /// <summary>Vertical breathing room kept between the outermost port and the body edge.</summary>
    public const double BodyVerticalPadding = 10.0;

    // Why: the two framework-seeded PortDirections this renderer has column geometry for. Compared
    // by Name (Ordinal) rather than against a cached PortDirections.ByName(...) option, because the
    // TypeCollection is populated by the entry-point app's module initializers — a static field here
    // could capture the NotFound sentinel if it initialised before that registration ran.
    private const string InDirectionName = "In";
    private const string OutDirectionName = "Out";

    /// <summary>
    /// Builds the port layout for a node.
    /// </summary>
    /// <param name="node">The node whose ports should be positioned.</param>
    /// <param name="portAnchorHalfWidth">
    /// The horizontal offset at which this node type's ports sit, taken from its
    /// <see cref="NodeAppearance.PortAnchorHalfWidth"/> so ports land on the shape's own edge.
    /// </param>
    /// <returns>The computed layout. A node with no ports yields an empty layout at the default body height.</returns>
    public static NodePortLayout BuildLayout(ICanvasNode node, double portAnchorHalfWidth)
    {
        var inPorts = new List<ICanvasPort>();
        var outPorts = new List<ICanvasPort>();
        var unplaceablePorts = new List<ICanvasPort>();

        foreach (var port in node.Ports)
        {
            if (string.Equals(port.Direction.Name, InDirectionName, StringComparison.Ordinal))
                inPorts.Add(port);
            else if (string.Equals(port.Direction.Name, OutDirectionName, StringComparison.Ordinal))
                outPorts.Add(port);
            else
                unplaceablePorts.Add(port);
        }

        var placements = new List<PortPlacement>(inPorts.Count + outPorts.Count);
        AddColumn(placements, inPorts, -portAnchorHalfWidth);
        AddColumn(placements, outPorts, portAnchorHalfWidth);

        return new NodePortLayout(BodyHalfHeight(Math.Max(inPorts.Count, outPorts.Count)), placements, unplaceablePorts);
    }

    // Why: the body only needs to grow once a column holds more ports than the original box can
    // contain. At 0 or 1 ports the expression stays under DefaultBodyHalfHeight, so an ordinary node
    // renders at exactly its pre-port size and no existing canvas shifts.
    private static double BodyHalfHeight(int tallestColumnPortCount) =>
        Math.Max(
            DefaultBodyHalfHeight,
            (Math.Max(0, tallestColumnPortCount - 1) / 2.0 * PortSpacing) + BodyVerticalPadding);

    private static void AddColumn(List<PortPlacement> into, List<ICanvasPort> ports, double dx)
    {
        // Why: centre the column on the node — port i of n sits at (i - (n-1)/2) * spacing, which
        // puts a lone port on the node's centre line and an even count symmetrically astride it.
        for (var i = 0; i < ports.Count; i++)
            into.Add(new PortPlacement(ports[i], dx, (i - ((ports.Count - 1) / 2.0)) * PortSpacing));
    }
}
