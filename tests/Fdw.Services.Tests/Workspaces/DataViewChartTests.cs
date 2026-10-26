using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Schema;
using Fdw.Services.Workspaces.Endpoints;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-2365: a data view holds a chart type with its encoding or neither, and a half chart is refused by naming the missing half.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class DataViewChartTests
{
    private const string Bar = """{"bindings":{"category":"grade","value":"absences"}}""";

    private static readonly List<DataSetFieldConfiguration> Fields =
    [
        new DataSetFieldConfiguration { Name = "grade", TypeName = "String", DatumTypeId = DatumTypes.Categorical.Id },
        new DataSetFieldConfiguration { Name = "absences", TypeName = "String", DatumTypeId = DatumTypes.Measure.Id },
    ];

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("  ", null)]
    [InlineData(null, "   ")]
    public void A_view_with_no_chart_has_nothing_to_refuse(string? chartType, string? encoding)
    {
        DataViewChart.Reason(chartType, encoding, Fields).ShouldBeNull();
    }

    [Fact]
    public void A_whole_chart_is_checked_as_a_chart_is()
    {
        DataViewChart.Reason("Bar", Bar, Fields).ShouldBeNull();
        DataViewChart.Reason("Bar", """{"bindings":{"category":"nothere","value":"absences"}}""", Fields).ShouldNotBeNull();
        DataViewChart.Reason("NotAChart", Bar, Fields).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Bar", null, "encoding is missing")]
    [InlineData("Bar", "", "encoding is missing")]
    [InlineData(null, Bar, "chartType is missing")]
    [InlineData("  ", Bar, "chartType is missing")]
    public void A_half_chart_is_refused_by_naming_the_missing_half(string? chartType, string? encoding, string missing)
    {
        DataViewChart.Reason(chartType, encoding, Fields).ShouldNotBeNull().ShouldContain(missing);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("Bar", "Bar")]
    public void A_blank_half_is_stored_as_none_and_a_given_half_as_sent(string? sent, string? stored)
    {
        DataViewChart.Given(sent).ShouldBe(stored);
    }
}
