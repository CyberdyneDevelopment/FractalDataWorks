namespace Fdw.Schema.Abstractions.Tests;

/// <summary>FDW-1032: a datum type says which chart axes it can sit on and which aggregates it allows.</summary>
public sealed class DatumTypeAxesTests
{
    [Theory]
    [InlineData("Attribute", "Dimension")]
    [InlineData("Categorical", "Dimension")]
    [InlineData("Identifier", "Dimension")]
    [InlineData("Metadata", "Dimension")]
    [InlineData("NotYetTyped", "Dimension")]
    [InlineData("Date", "Time")]
    [InlineData("LowerDateBoundary", "Time")]
    [InlineData("UpperDateBoundary", "Time")]
    [InlineData("Measure", "Measure")]
    [InlineData("Place", "Place")]
    public void ATypeSitsOnTheAxisItsFlagsSay(string datumType, string axis)
        => DatumTypes.ByName(datumType).Axes.ShouldBe([axis]);

    [Theory]
    [InlineData("DbKey")]
    [InlineData("Descriptive")]
    public void ATypeThatCannotBeGroupedOrPlottedSitsOnNoAxis(string datumType)
        => DatumTypes.ByName(datumType).Axes.ShouldBeEmpty();

    [Fact]
    public void AnAttributeIsNeverAMeasureAsItStands()
        => DatumTypes.Attribute.AcceptsAxis(AxisTypes.Measure.Name).ShouldBeFalse();

    [Fact]
    public void AnAttributeReachesTheValueOfAChartOnlyThroughAnAggregate()
    {
        DatumTypes.Attribute.AllowsAggregate(DatumAggregates.Count).ShouldBeTrue();
        DatumTypes.Attribute.AllowsAggregate(DatumAggregates.Sum).ShouldBeFalse();
        DatumTypes.Attribute.AllowsAggregate(DatumAggregates.Avg).ShouldBeFalse();
    }

    [Fact]
    public void AMeasureAllowsEveryAggregate()
        => DatumTypes.Measure.Aggregates.ShouldBe(
            [DatumAggregates.Count, DatumAggregates.Sum, DatumAggregates.Avg, DatumAggregates.Min, DatumAggregates.Max]);

    [Fact]
    public void ADescriptiveTypeAllowsOnlyCount()
        => DatumTypes.Descriptive.Aggregates.ShouldBe([DatumAggregates.Count]);

    [Fact]
    public void AnAxisAndAnAggregateAreComparedIgnoringCase()
    {
        DatumTypes.Date.AcceptsAxis("time").ShouldBeTrue();
        DatumTypes.Measure.AllowsAggregate("sum").ShouldBeTrue();
    }

    [Fact]
    public void EveryAxisADatumTypeCitesIsAnAxisType()
    {
        foreach (var type in DatumTypes.All())
            foreach (var axis in type.Axes)
                ReferenceEquals(AxisTypes.ByName(axis), AxisTypes.NotFound).ShouldBeFalse($"{type.Name} cites unknown axis '{axis}'");
    }
}
