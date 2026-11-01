using Fdw.Collections;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Interface for chart encoding role type options.
/// </summary>
/// <remarks>
/// <para>
/// Each encoding role represents one data-binding channel that a chart type can consume —
/// for example X axis, Y axis, series grouping, colour, or size. Roles carry a human-readable
/// display name and a flag indicating whether the role is spatial (axis-based) or visual
/// (colour, size, shape). Renderers use these flags to decide how to map bound fields to
/// chart-library APIs without any switch/if-else on the role name.
/// </para>
/// <para>
/// Compare against <see cref="ChartEncodingRoles.NotFound"/> — never null.
/// </para>
/// </remarks>
public interface IChartEncodingRole : ITypeOption<int, ChartEncodingRoleBase>
{
    /// <summary>
    /// Gets the human-readable display name for this encoding role (e.g. "X Axis", "Series").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether this role is a spatial (axis) channel.
    /// </summary>
    /// <remarks>
    /// Spatial roles (X, Y) map to axes; visual roles (Color, Size, Series, Region, Tooltip)
    /// map to legend / style properties. Renderers use this flag rather than name-matching.
    /// </remarks>
    bool IsSpatial { get; }
}
