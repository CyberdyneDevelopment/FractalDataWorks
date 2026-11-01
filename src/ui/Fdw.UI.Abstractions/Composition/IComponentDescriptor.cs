using System;

namespace Fdw.UI.Abstractions.Composition;

/// <summary>
/// Describes a composable UI component so a view can place it BY KEY rather than by markup.
/// </summary>
/// <remarks>
/// <para>
/// This is the missing rung in the headless architecture. FDW already has ~70 provider components
/// that each own their data and expose it through <c>RenderFragment&lt;TContext&gt;</c> — genuinely
/// composable units. What it had no way to express was "here is the set of them", so every view
/// was a hand-wired page that named its parts in markup. A user-arranged view cannot be built that
/// way: the arrangement is data, chosen at runtime, so the parts must be addressable at runtime too.
/// </para>
/// <para>
/// A descriptor is deliberately not a component. It is the metadata a palette needs to offer the
/// component, and a layout host needs to instantiate it — provider type, presentation, and sizing
/// hints — kept separate so enumerating the catalogue never means constructing every component in it.
/// </para>
/// </remarks>
public interface IComponentDescriptor
{
    /// <summary>
    /// Gets the stable key a saved layout stores to refer to this component.
    /// </summary>
    /// <remarks>
    /// Stability is the whole contract: a persisted arrangement outlives the code that produced it,
    /// so renaming a key orphans every layout that placed it. Treat it as permanent once shipped.
    /// </remarks>
    string Key { get; }

    /// <summary>Gets the human-readable name shown in a component palette.</summary>
    string DisplayName { get; }

    /// <summary>Gets the grouping used to organise a palette (e.g. "Connections", "Operations").</summary>
    string Category { get; }

    /// <summary>Gets a short description of what the component shows.</summary>
    string Description { get; }

    /// <summary>
    /// Gets the component type to instantiate — the headless provider, or a skin wrapping one.
    /// </summary>
    Type ComponentType { get; }

    /// <summary>Gets the default width in grid columns when first placed.</summary>
    int DefaultWidth { get; }

    /// <summary>Gets the default height in grid rows when first placed.</summary>
    int DefaultHeight { get; }

    /// <summary>Gets the smallest width in grid columns at which the component stays usable.</summary>
    int MinimumWidth { get; }

    /// <summary>Gets the smallest height in grid rows at which the component stays usable.</summary>
    int MinimumHeight { get; }
}
