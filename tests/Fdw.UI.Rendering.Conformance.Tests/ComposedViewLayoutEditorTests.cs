using System;
using System.Linq;
using Fdw.UI.Abstractions.Composition;
using Shouldly;
using Xunit;

namespace Fdw.UI.Rendering.Conformance.Tests;

/// <summary>
/// Covers the arrangement rules behind drag-to-move, drag-to-resize, add-from-palette, and remove.
/// </summary>
/// <remarks>
/// These live outside the Blazor host precisely so they can be asserted directly: bounds, minimum
/// sizes, and free-cell search are real logic, and logic that only exists inside a .razor file gets
/// verified by clicking rather than by tests.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "Rendering")]
public class ComposedViewLayoutEditorTests
{
    private static ComposedViewLayout Layout(int columns = 12) =>
        new() { ViewId = "v", ColumnCount = columns };

    private static PlacedComponent Place(ComposedViewLayout layout, int col, int row, int w, int h)
    {
        var placement = new PlacedComponent
        {
            Id = Guid.NewGuid(), ComponentKey = "k", Column = col, Row = row, Width = w, Height = h,
        };
        layout.Components.Add(placement);
        return placement;
    }

    private sealed class TestDescriptor : ComponentDescriptorBase
    {
        public TestDescriptor(int defaultW = 4, int defaultH = 3, int minW = 2, int minH = 2)
            : base(1, "test", "Test", "General", "A test component")
        {
            DefaultWidth = defaultW; DefaultHeight = defaultH;
            MinimumWidth = minW; MinimumHeight = minH;
        }

        public override Type ComponentType => typeof(TestDescriptor);

        public override int DefaultWidth { get; }

        public override int DefaultHeight { get; }

        public override int MinimumWidth { get; }

        public override int MinimumHeight { get; }
    }

    [Fact]
    public void MoveRepositionsThePlacement()
    {
        var layout = Layout();
        var placement = Place(layout, 0, 0, 4, 2);

        ComposedViewLayoutEditor.Move(layout, placement.Id, 3, 5).ShouldBeTrue();
        placement.Column.ShouldBe(3);
        placement.Row.ShouldBe(5);
    }

    [Fact]
    public void MovePastTheRightEdgeIsRefusedAndChangesNothing()
    {
        var layout = Layout(columns: 12);
        var placement = Place(layout, 0, 0, 4, 2);

        // Column 9 + width 4 = 13 > 12. Refusing beats clamping: quietly snapping the placement
        // somewhere the user did not point at reads as the UI ignoring the gesture.
        ComposedViewLayoutEditor.Move(layout, placement.Id, 9, 0).ShouldBeFalse();
        placement.Column.ShouldBe(0);
    }

    [Fact]
    public void MoveOfAnUnknownPlacementIsRefused()
    {
        var layout = Layout();
        Place(layout, 0, 0, 4, 2);

        ComposedViewLayoutEditor.Move(layout, Guid.NewGuid(), 1, 1).ShouldBeFalse();
    }

    [Fact]
    public void ResizeAppliesWithinBoundsAndMinimums()
    {
        var layout = Layout();
        var placement = Place(layout, 0, 0, 4, 2);

        ComposedViewLayoutEditor.Resize(layout, placement.Id, 6, 4, new TestDescriptor()).ShouldBeTrue();
        placement.Width.ShouldBe(6);
        placement.Height.ShouldBe(4);
    }

    [Fact]
    public void ResizeBelowTheComponentsMinimumIsRefused()
    {
        var layout = Layout();
        var placement = Place(layout, 0, 0, 4, 3);

        // The catalogue already declared this size unusable; honouring the drag would produce a
        // placement the component itself says does not work.
        ComposedViewLayoutEditor.Resize(layout, placement.Id, 1, 3, new TestDescriptor(minW: 2, minH: 2))
            .ShouldBeFalse();
        placement.Width.ShouldBe(4);
    }

    [Fact]
    public void ResizePastTheRightEdgeIsRefused()
    {
        var layout = Layout(columns: 12);
        var placement = Place(layout, 8, 0, 4, 2);

        ComposedViewLayoutEditor.Resize(layout, placement.Id, 6, 2, new TestDescriptor()).ShouldBeFalse();
        placement.Width.ShouldBe(4);
    }

    [Fact]
    public void AddPlacesTheFirstComponentAtTheOrigin()
    {
        var layout = Layout();

        var placement = ComposedViewLayoutEditor.Add(layout, new TestDescriptor(), Guid.NewGuid());

        placement.ShouldNotBeNull();
        placement!.Column.ShouldBe(0);
        placement.Row.ShouldBe(0);
        layout.Components.Count.ShouldBe(1);
    }

    [Fact]
    public void AddFindsAFreeCellRatherThanOverlapping()
    {
        var layout = Layout(columns: 12);
        Place(layout, 0, 0, 6, 3);

        var placement = ComposedViewLayoutEditor.Add(layout, new TestDescriptor(defaultW: 4, defaultH: 3), Guid.NewGuid());

        placement.ShouldNotBeNull();
        layout.Components
            .Where(c => c.Id != placement!.Id)
            .ShouldAllBe(existing => !ComposedViewLayoutEditor.Overlaps(placement!, existing));
    }

    [Fact]
    public void AddRefusesAComponentWiderThanTheGrid()
    {
        var layout = Layout(columns: 4);

        // Shrinking it to fit would place a component below the width it declared readable.
        ComposedViewLayoutEditor.Add(layout, new TestDescriptor(defaultW: 8, minW: 6), Guid.NewGuid())
            .ShouldBeNull();
        layout.Components.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveDeletesOnlyTheNamedPlacement()
    {
        var layout = Layout();
        var first = Place(layout, 0, 0, 4, 2);
        var second = Place(layout, 4, 0, 4, 2);

        ComposedViewLayoutEditor.Remove(layout, first.Id).ShouldBeTrue();
        layout.Components.Count.ShouldBe(1);
        layout.Components[0].Id.ShouldBe(second.Id);
    }

    [Fact]
    public void RemoveOfAnUnknownPlacementIsRefused()
    {
        var layout = Layout();
        Place(layout, 0, 0, 4, 2);

        ComposedViewLayoutEditor.Remove(layout, Guid.NewGuid()).ShouldBeFalse();
        layout.Components.Count.ShouldBe(1);
    }

    [Fact]
    public void OverlapDetectionIsExclusiveAtTheEdges()
    {
        var layout = Layout();
        var left = Place(layout, 0, 0, 4, 2);
        var adjacent = Place(layout, 4, 0, 4, 2);
        var overlapping = Place(layout, 3, 1, 4, 2);

        // Adjacent placements share an edge but not an area — treating that as an overlap would
        // make it impossible to tile a row.
        ComposedViewLayoutEditor.Overlaps(left, adjacent).ShouldBeFalse();
        ComposedViewLayoutEditor.Overlaps(left, overlapping).ShouldBeTrue();
    }
}
