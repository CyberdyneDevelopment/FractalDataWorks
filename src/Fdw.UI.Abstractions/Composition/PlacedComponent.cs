using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Composition;

/// <summary>
/// One component placed on a composed view: which component, where, how big, and its instance settings.
/// </summary>
public sealed class PlacedComponent
{
    /// <summary>
    /// Gets or sets the placement's own identifier, unique within its view.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="ComponentKey"/> because the same component can legitimately appear
    /// more than once in one view — two connection panels watching different connections — and each
    /// placement then needs its own identity to be moved, resized, or removed independently.
    /// </remarks>
    /// <remarks>Assigned by whatever creates the placement; there is no generated default here,
    /// so an unset Id is visibly empty rather than quietly unique.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the catalogue key of the component to render.</summary>
    public string ComponentKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the zero-based grid column of the placement's left edge.</summary>
    public int Column { get; set; }

    /// <summary>Gets or sets the zero-based grid row of the placement's top edge.</summary>
    public int Row { get; set; }

    /// <summary>Gets or sets the width in grid columns.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the height in grid rows.</summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets this placement's own settings — which connection, which metric, and so on.
    /// </summary>
    /// <remarks>
    /// Held as strings rather than typed values because the layout is persisted as JSON and read
    /// back without knowing which component it belongs to. The component owns interpretation, and
    /// must fail loud on a setting it cannot read rather than substituting a default: a widget
    /// silently showing the wrong connection is worse than one saying it was configured wrong.
    /// </remarks>
    public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
