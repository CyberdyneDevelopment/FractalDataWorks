using System.Linq;
using Fdw.Schema;
using Fdw.Services.Workspaces.Charts;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Charts;

/// <summary>FDW-1023: the chart types and their slots are well formed, so a saved chart is checked against something that can be satisfied.</summary>
[Collection(nameof(ServicesTestCollection))]
public class ChartTypesTests
{
    private static System.Collections.Generic.IEnumerable<IChartType> All()
        => ChartTypes.All().Where(t => !ReferenceEquals(t, ChartTypes.NotFound));

    [Fact]
    public void TheNineChartTypesTheConsoleOffersExist()
        => All().Select(t => t.Name).OrderBy(n => n).ToArray().ShouldBe(
            new[] { "Area", "Bar", "Donut", "Heatmap", "Line", "Map", "Scatter", "StackedBar", "Treemap" });

    [Fact]
    public void EveryChartTypeHasAWayOfBeingBoundAndEveryWayNeedsAField()
    {
        foreach (var type in All())
        {
            type.Shapes.ShouldNotBeEmpty(type.Name);
            foreach (var shape in type.Shapes)
                shape.Slots.Any(s => s.IsRequired).ShouldBeTrue($"{type.Name}/{shape.Name} has no required slot");
        }
    }

    [Fact]
    public void ASlotNameIsUniqueWithinItsShape()
    {
        foreach (var shape in All().SelectMany(t => t.Shapes))
            shape.Slots.Select(s => s.Name.ToLowerInvariant()).Distinct().Count().ShouldBe(shape.Slots.Count, shape.Name);
    }

    [Fact]
    public void EverySlotCitesOnlyAxesThatExist()
    {
        foreach (var slot in All().SelectMany(t => t.Shapes).SelectMany(s => s.Slots))
        {
            slot.Axes.ShouldNotBeEmpty(slot.Name);
            foreach (var axis in slot.Axes)
                ReferenceEquals(AxisTypes.ByName(axis), AxisTypes.NotFound).ShouldBeFalse($"{slot.Name} cites unknown axis '{axis}'");
        }
    }

    [Fact]
    public void EveryChartTypeSaysWhatItIsFor()
    {
        foreach (var type in All())
            type.Description.ShouldNotBeNullOrWhiteSpace(type.Name);
    }

    [Fact]
    public void AMapBindsLongitudeAndLatitudeTogetherOrOnePlaceFieldAlone()
    {
        var shapes = ChartTypes.ByName("Map").Shapes;

        shapes.Count.ShouldBe(2);
        shapes[0].Slots.Where(s => s.IsRequired).Select(s => s.Name).ToArray().ShouldBe(new[] { "map.x", "map.y" });
        shapes[1].Slots.Where(s => s.IsRequired).Select(s => s.Name).ToArray().ShouldBe(new[] { "map.place" });
    }

    [Fact]
    public void ANameIsExactSoALowerCaseNameIsNotAChartType()
        => ReferenceEquals(ChartTypes.ByName("bar"), ChartTypes.NotFound).ShouldBeTrue();
}
