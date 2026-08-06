namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Outcome of a single <see cref="BrowsePageRenderer.Render"/> call.</summary>
/// <param name="Action">Which action the user chose.</param>
/// <param name="SelectedIndex">The index of the selected item in the active column, or -1 when not applicable.</param>
/// <param name="Payload">The opaque payload of the selected item (when <see cref="Action"/> is <see cref="BrowseAction.DrillDown"/>).</param>
public sealed record BrowsePageResult(BrowseAction Action, int SelectedIndex, object? Payload);
