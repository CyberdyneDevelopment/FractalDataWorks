using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Schema;
using Fdw.Services.Workspaces.Charts;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Charts;

/// <summary>
/// FDW-1023: a saved chart is refused unless it can be drawn, and the refusal names the slot or the field.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class ChartSpecificationCheckTests
{
    private static readonly List<DataSetFieldConfiguration> Fields =
    [
        Field("grade", DatumTypes.Categorical),
        Field("campus", DatumTypes.Categorical),
        Field("week", DatumTypes.Date),
        Field("absences", DatumTypes.Measure),
        Field("excused", DatumTypes.Measure),
        Field("longitude", DatumTypes.Place),
        Field("latitude", DatumTypes.Place),
        Field("district", DatumTypes.Place),
        Field("note", DatumTypes.Descriptive),
        new DataSetFieldConfiguration { Name = "retired", TypeName = "String", DatumTypeId = DatumTypes.Categorical.Id, IsDeleted = true },
    ];

    private static string? Reason(string chartType, string encoding) => ChartSpecificationCheck.Reason(chartType, encoding, Fields);

    [Theory]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":["absences","excused"],"series":null},"options":{"stacked":false}}""")]
    [InlineData("Bar", """{"bindings":{"category":"GRADE","value":"absences"}}""")]
    [InlineData("Bar", """{"bindings":{"category":"week","value":"absences"}}""")]
    [InlineData("StackedBar", """{"bindings":{"category":"grade","value":["absences","excused"],"series":"campus"}}""")]
    [InlineData("StackedBar", """{"bindings":{"category":"week","value":["absences","excused"]}}""")]
    [InlineData("Line", """{"bindings":{"category":"week","value":["absences","excused"]}}""")]
    [InlineData("Line", """{"bindings":{"category":"grade","value":"absences"}}""")]
    [InlineData("Area", """{"bindings":{"category":"week","value":"absences","series":"campus"}}""")]
    [InlineData("Scatter", """{"bindings":{"x":"absences","y":"excused","size":null,"colour":"campus"}}""")]
    [InlineData("Donut", """{"bindings":{"slice":"campus","value":"absences"}}""")]
    [InlineData("Heatmap", """{"bindings":{"x":"week","y":"grade","value":"absences"}}""")]
    [InlineData("Treemap", """{"bindings":{"group":"campus","subgroup":"grade","size":"absences"}}""")]
    [InlineData("Map", """{"bindings":{"map.x":"longitude","map.y":"latitude"}}""")]
    [InlineData("Map", """{"bindings":{"map.x":"longitude","map.y":"latitude","map.z":"absences"}}""")]
    [InlineData("Map", """{"bindings":{"map.place":"district","map.z":"absences"}}""")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"aggregate":"Count"}}}""")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"field":"campus","aggregate":"Count"}}}""")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":[{"field":"absences","aggregate":"Sum"},{"field":"excused","aggregate":"Avg"}]}}""")]
    [InlineData("Donut", """{"bindings":{"slice":"campus","value":{"field":"absences","aggregate":"max"}}}""")]
    [InlineData("Map", """{"bindings":{"map.place":"district","map.z":{"field":"note","aggregate":"Count"}}}""")]
    public void AChartThatFitsIsAccepted(string chartType, string encoding)
        => Reason(chartType, encoding).ShouldBeNull();

    [Theory]
    [InlineData("bar", """{"bindings":{}}""", "chart type 'bar' is not one of")]
    [InlineData("Pie", """{"bindings":{}}""", "Donut")]
    [InlineData("Bar", "not json", "not valid JSON")]
    [InlineData("Bar", "[1]", "must be a JSON object")]
    [InlineData("Bar", """{"binding":{"category":"grade"}}""", "key 'binding'")]
    [InlineData("Bar", """{"options":{}}""", "must have 'bindings'")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":"absences"},"options":3}""", "'options' must be an object")]
    [InlineData("Bar", """{"bindings":{"category":"grade"}}""", "slot 'value' needs a field")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":"absences","colour":"campus"}}""", "no slot 'colour'")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":3}}""", "slot 'value' must be a field name")]
    [InlineData("Bar", """{"bindings":{"category":["grade","campus"],"value":"absences"}}""", "slot 'category' takes one field, not 2")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":"gone"}}""", "slot 'value' names field 'gone', which the data set does not have")]
    [InlineData("Bar", """{"bindings":{"category":"retired","value":"absences"}}""", "field 'retired', which the data set does not have")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":"campus"}}""", "slot 'value' needs a Measure field, but 'campus' is typed Categorical")]
    [InlineData("Bar", """{"bindings":{"category":"absences","value":"excused"}}""", "slot 'category' needs a Dimension or Time field, but 'absences' is typed Measure")]
    [InlineData("Donut", """{"bindings":{"slice":"week","value":"absences"}}""", "slot 'slice' needs a Dimension field, but 'week' is typed Date")]
    [InlineData("Bar", """{"bindings":{"category":"district","value":"absences"}}""", "typed Place")]
    [InlineData("Map", """{"bindings":{"map.x":"longitude"}}""", "slot 'map.y' needs a field")]
    [InlineData("Map", """{"bindings":{"map.x":"longitude","map.y":"latitude","map.place":"district"}}""", "binds map.x + map.y")]
    [InlineData("Map", """{"bindings":{}}""", "or map.place")]
    [InlineData("Donut", """{"bindings":{"slice":"campus","value":["absences","excused"]}}""", "slot 'value' takes one field, not 2")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"field":"campus","aggregate":"Sum"}}}""", "cannot plot Sum of 'campus': it is typed Categorical, which allows Count, Min, Max")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":"campus"}}""", "as a value it needs an aggregate, which Categorical allows as Count, Min, Max")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"field":"absences"}}}""", "must name an 'aggregate'")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"aggregate":"Sum"}}}""", "Sum needs a field; only Count may name none")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"field":"absences","aggregate":"Median"}}}""", "aggregate 'Median' is not one of Count, Sum, Avg, Min, Max")]
    [InlineData("Bar", """{"bindings":{"category":{"field":"grade","aggregate":"Count"},"value":"absences"}}""", "slot 'category' takes a field as it stands, not an aggregate")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"field":"week","aggregate":"Max"}}}""", "so the result is not a number")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"field":"absences","aggregate":"Sum","extra":"x"}}}""", "key 'extra'")]
    [InlineData("Bar", """{"bindings":{"category":"grade","value":{"field":"note","aggregate":"Sum"}}}""", "typed Descriptive, which allows Count")]
    public void AChartThatDoesNotFitIsRefusedNamingTheSlotOrField(string chartType, string encoding, string expected)
        => Reason(chartType, encoding).ShouldNotBeNull().ShouldContain(expected);

    [Fact]
    public void ASlotBoundTwiceUnderTwoCasesIsRefused()
        => Reason("Bar", """{"bindings":{"category":"grade","Category":"campus","value":"absences"}}""")
            .ShouldNotBeNull().ShouldContain("bound twice");

    private static DataSetFieldConfiguration Field(string name, IDatumType datum)
        => new() { Name = name, TypeName = "String", DatumTypeId = datum.Id };
}
