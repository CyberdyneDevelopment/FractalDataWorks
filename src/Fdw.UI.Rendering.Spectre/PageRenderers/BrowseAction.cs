namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>High-level actions the user can take on a browse page.</summary>
// Why: FDW017 recommends TypeCollections over enums. Leaving as enum intentionally —
// this is an internal renderer contract, not extended at runtime, and there is no
// dispatch logic that would benefit from TypeCollection lookup.
#pragma warning disable FDW017
public enum BrowseAction
{
    /// <summary>Quit the browse session.</summary>
    Quit = 0,

    /// <summary>Drill into the selected item (load the next column).</summary>
    DrillDown = 1,

    /// <summary>Step back to the parent column.</summary>
    Back = 2,

    /// <summary>Reload the active column's items.</summary>
    Refresh = 3,
}
#pragma warning restore FDW017
