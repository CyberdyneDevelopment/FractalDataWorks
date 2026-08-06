using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// The computed port geometry for a single canvas node: where each port sits relative to the
/// node's centre, and how tall the node body must be to contain them.
/// </summary>
/// <remarks>
/// Built by <see cref="SvgPortGeometry.BuildLayout"/> once per node per render pass. Ports whose
/// <see cref="IPortDirection"/> is neither In nor Out are surfaced in
/// <see cref="UnplaceablePorts"/> rather than being quietly dropped — the renderer reports them.
/// </remarks>
internal sealed class NodePortLayout
{
    // Why: Ordinal — ICanvasPort.Id is an exact internal key (e.g. "in:CustomerId"), not user text.
    private readonly Dictionary<string, PortPlacement> _byPortId;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodePortLayout"/> class.
    /// </summary>
    /// <param name="bodyHalfHeight">Half the height the node body must occupy to contain its ports.</param>
    /// <param name="placements">Every placeable port with its computed offset.</param>
    /// <param name="unplaceablePorts">Ports whose direction has no defined geometry in this renderer.</param>
    public NodePortLayout(
        double bodyHalfHeight,
        IReadOnlyList<PortPlacement> placements,
        IReadOnlyList<ICanvasPort> unplaceablePorts)
    {
        BodyHalfHeight = bodyHalfHeight;
        Placements = placements;
        UnplaceablePorts = unplaceablePorts;

        _byPortId = new Dictionary<string, PortPlacement>(placements.Count, StringComparer.Ordinal);
        foreach (var placement in placements)
            _byPortId[placement.Port.Id] = placement;
    }

    /// <summary>
    /// Gets half the height of the node body, grown from the renderer's default so the tallest
    /// port column fits inside the shape.
    /// </summary>
    public double BodyHalfHeight { get; }

    /// <summary>
    /// Gets every port that has a computed position, in render order (In column then Out column).
    /// </summary>
    public IReadOnlyList<PortPlacement> Placements { get; }

    /// <summary>
    /// Gets the ports this renderer has no geometry for — a port direction beyond the framework's
    /// seeded In/Out (see <see cref="PortDirections"/>, which downstream assemblies may extend).
    /// </summary>
    public IReadOnlyList<ICanvasPort> UnplaceablePorts { get; }

    /// <summary>
    /// Looks up a port's placement by its identifier.
    /// </summary>
    /// <param name="portId">The port identifier to resolve.</param>
    /// <param name="placement">The resolved placement when found.</param>
    /// <returns><c>true</c> when the node exposes a port with this identifier; otherwise <c>false</c>.</returns>
    public bool TryGetPlacement(string portId, [MaybeNullWhen(false)] out PortPlacement placement) =>
        _byPortId.TryGetValue(portId, out placement);
}
