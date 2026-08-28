using System;
using System.Linq;

namespace Fdw.UI.Abstractions.Composition;

/// <summary>
/// Pure layout-editing operations: add, move, resize, and remove placements.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately separate from the Blazor host that drives it. Arranging a view is real logic —
/// bounds, minimum sizes, finding somewhere to put a new component — and logic buried in a
/// <c>.razor</c> file cannot be unit-tested, so it ends up verified only by clicking. Keeping it
/// here means the rules are tested directly and any host (drag-and-drop, keyboard, an API) enforces
/// exactly the same ones.
/// </para>
/// <para>
/// Every operation mutates the supplied layout in place and reports whether it applied. A rejected
/// edit changes nothing — it never silently clamps into a different edit than the one requested, so
/// a caller can distinguish "moved" from "refused to move" and tell the user which.
/// </para>
/// </remarks>
public static class ComposedViewLayoutEditor
{
    /// <summary>
    /// Adds a component to the layout at the first position where it fits.
    /// </summary>
    /// <param name="layout">The layout to add to.</param>
    /// <param name="descriptor">The catalogue entry being placed.</param>
    /// <param name="placementId">The identity to give the new placement.</param>
    /// <returns>The new placement, or <see langword="null"/> when the component is wider than the grid.</returns>
    /// <remarks>
    /// Returns null rather than shrinking an oversized component to fit: a component that declares
    /// a minimum width does so because it stops being readable below it, and silently placing an
    /// unusable one is worse than declining and saying why.
    /// </remarks>
    public static PlacedComponent? Add(ComposedViewLayout layout, IComponentDescriptor descriptor, Guid placementId)
    {
        if (layout is null || descriptor is null)
        {
            return null;
        }

        var width = Math.Max(descriptor.DefaultWidth, descriptor.MinimumWidth);
        var height = Math.Max(descriptor.DefaultHeight, descriptor.MinimumHeight);
        if (width > layout.ColumnCount)
        {
            return null;
        }

        var (column, row) = FindFreeCell(layout, width, height);
        var placement = new PlacedComponent
        {
            Id = placementId,
            ComponentKey = descriptor.Key,
            Column = column,
            Row = row,
            Width = width,
            Height = height,
        };

        layout.Components.Add(placement);
        return placement;
    }

    /// <summary>
    /// Moves a placement so its top-left sits at the supplied cell.
    /// </summary>
    /// <param name="layout">The layout being edited.</param>
    /// <param name="placementId">The placement to move.</param>
    /// <param name="column">The target zero-based column.</param>
    /// <param name="row">The target zero-based row.</param>
    /// <returns><see langword="true"/> when the move applied.</returns>
    public static bool Move(ComposedViewLayout layout, Guid placementId, int column, int row)
    {
        var placement = Find(layout, placementId);
        if (placement is null || column < 0 || row < 0)
        {
            return false;
        }

        if (column + placement.Width > layout.ColumnCount)
        {
            return false;
        }

        placement.Column = column;
        placement.Row = row;
        return true;
    }

    /// <summary>
    /// Resizes a placement, honouring the component's declared minimums and the grid width.
    /// </summary>
    /// <param name="layout">The layout being edited.</param>
    /// <param name="placementId">The placement to resize.</param>
    /// <param name="width">The requested width in grid columns.</param>
    /// <param name="height">The requested height in grid rows.</param>
    /// <param name="descriptor">
    /// The placement's catalogue entry, supplying the minimum size. When null, only the grid bounds
    /// are enforced.
    /// </param>
    /// <returns><see langword="true"/> when the resize applied.</returns>
    public static bool Resize(
        ComposedViewLayout layout,
        Guid placementId,
        int width,
        int height,
        IComponentDescriptor? descriptor)
    {
        var placement = Find(layout, placementId);
        if (placement is null || width < 1 || height < 1)
        {
            return false;
        }

        if (descriptor is not null && (width < descriptor.MinimumWidth || height < descriptor.MinimumHeight))
        {
            return false;
        }

        if (placement.Column + width > layout.ColumnCount)
        {
            return false;
        }

        placement.Width = width;
        placement.Height = height;
        return true;
    }

    /// <summary>
    /// Removes a placement from the layout.
    /// </summary>
    /// <param name="layout">The layout being edited.</param>
    /// <param name="placementId">The placement to remove.</param>
    /// <returns><see langword="true"/> when a placement was removed.</returns>
    public static bool Remove(ComposedViewLayout layout, Guid placementId)
    {
        var placement = Find(layout, placementId);
        if (placement is null)
        {
            return false;
        }

        layout.Components.Remove(placement);
        return true;
    }

    /// <summary>
    /// Determines whether two placements overlap.
    /// </summary>
    /// <param name="first">The first placement.</param>
    /// <param name="second">The second placement.</param>
    /// <returns><see langword="true"/> when their rectangles intersect.</returns>
    public static bool Overlaps(PlacedComponent first, PlacedComponent second) =>
        first is not null && second is not null &&
        first.Column < second.Column + second.Width &&
        second.Column < first.Column + first.Width &&
        first.Row < second.Row + second.Height &&
        second.Row < first.Row + first.Height;

    private static PlacedComponent? Find(ComposedViewLayout layout, Guid placementId) =>
        layout?.Components.FirstOrDefault(c => c.Id == placementId);

    private static (int Column, int Row) FindFreeCell(ComposedViewLayout layout, int width, int height)
    {
        for (var row = 0; row < MaxSearchRows(layout); row++)
        {
            for (var column = 0; column + width <= layout.ColumnCount; column++)
            {
                var candidate = new PlacedComponent { Column = column, Row = row, Width = width, Height = height };
                if (!layout.Components.Any(existing => Overlaps(candidate, existing)))
                {
                    return (column, row);
                }
            }
        }

        return (0, MaxSearchRows(layout));
    }

    private static int MaxSearchRows(ComposedViewLayout layout) =>
        layout.Components.Count == 0 ? 1 : layout.Components.Max(c => c.Row + c.Height) + 1;
}
