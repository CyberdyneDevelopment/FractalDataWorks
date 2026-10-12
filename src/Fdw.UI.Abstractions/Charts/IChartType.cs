using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Interface for chart type options.
/// </summary>
/// <remarks>
/// <para>
/// Each registered chart type carries its display metadata and a data-driven declaration of which
/// encoding roles it requires and which it accepts optionally. Renderers and the field-binding UI
/// use these lists to filter roles without any switch/if-else on the chart type name.
/// </para>
/// <para>
/// Compare against <see cref="ChartTypes.NotFound"/> — never null.
/// </para>
/// </remarks>
public interface IChartType : ITypeOption<int, ChartTypeBase>
{
    /// <summary>
    /// Gets the human-readable display name shown in the chart-type picker (e.g. "Bar Chart").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets an icon hint string passed to the renderer (e.g. a Lucide icon name, an SVG id).
    /// </summary>
    /// <remarks>
    /// The meaning of this string is a convention between the domain that seeds chart types and
    /// the renderer implementation. The chart contract layer does not interpret it.
    /// </remarks>
    string IconHint { get; }

    /// <summary>
    /// Gets the encoding roles that MUST be bound for this chart type to render meaningfully.
    /// </summary>
    /// <remarks>
    /// The role names correspond to entries in <see cref="ChartEncodingRoles"/>. Hosts validate
    /// that every required role has a bound <see cref="ChartEncoding"/> before invoking the renderer.
    /// </remarks>
    IReadOnlyList<string> RequiredEncodings { get; }

    /// <summary>
    /// Gets the encoding roles that may optionally be bound to enhance this chart type.
    /// </summary>
    /// <remarks>
    /// The role names correspond to entries in <see cref="ChartEncodingRoles"/>. Unbound optional
    /// roles are silently ignored by the renderer.
    /// </remarks>
    IReadOnlyList<string> OptionalEncodings { get; }
}
