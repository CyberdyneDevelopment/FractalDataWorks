using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Composition;

/// <summary>
/// A user's arrangement of components into a view — the unit that gets saved and restored.
/// </summary>
/// <remarks>
/// <para>
/// Persisted as JSON through the existing per-user <c>ISessionStateService</c> rather than a new
/// store: that service already provides exactly this (per-user, keyed, JSON) and is already wired,
/// so a layout is one more key beside the filter and view state the UI already keeps there.
/// </para>
/// <para>
/// Deliberately data-only, with no reference to a component instance or a renderer. A layout
/// outlives the session that produced it and may be read by a different renderer than the one that
/// wrote it, so anything that cannot survive serialisation does not belong on it.
/// </para>
/// </remarks>
public sealed class ComposedViewLayout
{
    /// <summary>The session-state key format for a user's layout of a given view.</summary>
    /// <remarks>
    /// Matches the <c>{domain}:{page}:{component}</c> convention ISessionStateService documents, so
    /// layouts sort and enumerate alongside the rest of a user's stored UI state.
    /// </remarks>
    public const string SessionStateKeyFormat = "layout:{0}:composition";

    /// <summary>Gets or sets the view identifier this layout arranges.</summary>
    public string ViewId { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-facing name of the view.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of columns the grid is divided into.</summary>
    /// <remarks>
    /// Stored with the layout rather than fixed by the host: placements are expressed in grid units,
    /// so the column count they were authored against is what makes those units mean anything. A
    /// host imposing its own would silently reflow every saved arrangement.
    /// </remarks>
    public int ColumnCount { get; set; } = 12;

    /// <summary>Gets or sets the placed components.</summary>
    public IList<PlacedComponent> Components { get; set; } = [];

    /// <summary>
    /// Builds the session-state key under which a view's layout is stored for a user.
    /// </summary>
    /// <param name="viewId">The view identifier.</param>
    /// <returns>The session-state key.</returns>
    public static string KeyFor(string viewId) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, SessionStateKeyFormat, viewId);
}
