using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Schema;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-1023: the grouped half of POST /data/{dataset} becomes exactly the grouped query asked for, or is refused
/// whole. What may be grouped and aggregated is decided by each field's datum type.
/// </summary>
public sealed class DataQueryGroupingTests
{
    [Fact]
    public void AGroupAndASumBecomeAnAggregationOverTheFieldsOwnNames()
    {
        var query = Grouped(new DataQueryRequest
        {
            GroupBy = ["Grade"],
            Aggregates = [new DataQueryAggregate { Function = "Sum", Field = "absences", As = "total" }],
        }, 12);

        query.IsSuccess.ShouldBeTrue();
        var aggregation = query.Value!.Command.Aggregation!;
        aggregation.GroupByFields.ToArray().ShouldBe(new[] { "grade" });
        aggregation.Aggregations["total"].ShouldBe("SUM(absences)");
        aggregation.IncludeGroupCount.ShouldBeTrue();
        query.Value.Command.Paging!.Take.ShouldBe(13);
    }

    [Fact]
    public void TheColumnsAreTheGroupFieldsThenEachAggregate()
    {
        var query = Grouped(new DataQueryRequest
        {
            GroupBy = ["district"],
            Aggregates =
            [
                new DataQueryAggregate { Function = "Sum", Field = "absences", As = "total" },
                new DataQueryAggregate { Function = "Count", As = "rows" },
                new DataQueryAggregate { Function = "Max", Field = "absences", As = "worst" },
            ],
        }, 5);

        query.Value!.Columns.Select(c => $"{c.Name}:{c.DataType}").ToArray()
            .ShouldBe(new[] { "district:String", "total:Decimal", "rows:Int64", "worst:Int64" });
        query.Value.Command.Aggregation!.Aggregations["rows"].ShouldBe("COUNT(*)");
    }

    [Fact]
    public void NoGroupFieldWithAnAggregateIsOneRowOverEveryRowTheFilterKeeps()
    {
        var query = Grouped(new DataQueryRequest
        {
            GroupBy = [],
            Aggregates = [new DataQueryAggregate { Function = "Sum", Field = "absences", As = "total" }],
        }, 5);

        query.IsSuccess.ShouldBeTrue();
        query.Value!.Command.Aggregation!.GroupByFields.ShouldBeEmpty();
        query.Value.Command.Aggregation.IncludeGroupCount.ShouldBeFalse();
    }

    [Fact]
    public void ASortMayNameAGroupFieldOrAnAggregatesName()
    {
        var query = Grouped(new DataQueryRequest
        {
            GroupBy = ["grade"],
            Aggregates = [new DataQueryAggregate { Function = "Sum", Field = "absences", As = "total" }],
            Sort = [new QuerySort { Field = "TOTAL", Direction = "Descending" }],
        }, 5);

        query.IsSuccess.ShouldBeTrue();
        query.Value!.Command.Ordering!.OrderedFields.Single().PropertyName.ShouldBe("total");
    }

    [Fact]
    public void TheFilterIsCheckedAgainstTheDataSetsFields()
    {
        var query = Grouped(new DataQueryRequest
        {
            GroupBy = ["grade"],
            Filter = new QueryFilterNode { Field = "district", Operator = "Equal", Value = Json("\"7\"") },
        }, 5);

        query.IsSuccess.ShouldBeTrue();
        query.Value!.Command.Filter!.Root.ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("district");
    }

    private static IEnumerable<DataQueryRequest> Refused() =>
    [
        // nothing to group or aggregate
        new DataQueryRequest { GroupBy = [] },
        // not a field of the data set
        new DataQueryRequest { GroupBy = ["nope"] },
        // computed after the rows are read, so the store has no column to group
        new DataQueryRequest { GroupBy = ["margin"] },
        // a measure is not grouped by, and a description is neither grouped nor summed
        new DataQueryRequest { GroupBy = ["absences"] },
        new DataQueryRequest { GroupBy = ["note"] },
        // a group field named twice
        new DataQueryRequest { GroupBy = ["grade", "GRADE"] },
        // a category is not summed
        Sum("grade", "total"),
        Sum("note", "total"),
        // a function that does not exist, a missing name, a name that is not plain, a name taken twice
        new DataQueryRequest { Aggregates = [new DataQueryAggregate { Function = "Median", Field = "absences", As = "m" }] },
        Sum("absences", ""),
        Sum("absences", "total absences"),
        Sum("absences", "1total"),
        Sum("absences", "grade", groupBy: ["grade"]),
        Sum("absences", "FdwGroupCount"),
        // only Count may name no field
        new DataQueryRequest { Aggregates = [new DataQueryAggregate { Function = "Sum", As = "total" }] },
        new DataQueryRequest { Aggregates = [null] },
        // a sort on something the query does not return
        new DataQueryRequest { GroupBy = ["grade"], Sort = [new QuerySort { Field = "district", Direction = "Ascending" }] },
        new DataQueryRequest { GroupBy = ["grade"], Sort = [new QuerySort { Field = "grade", Direction = "Sideways" }] },
        new DataQueryRequest { GroupBy = ["grade"], Skip = -1 },
    ];

    [Fact]
    public void ARequestThatDoesNotResolveIsRefusedWholeAsDataQueryInvalid()
    {
        var cases = Refused().ToList();
        for (var i = 0; i < cases.Count; i++)
            Grouped(cases[i], 5).IsFailure.ShouldBeTrue($"refused case #{i}");
    }

    [Fact]
    public void APlaceCanBeGroupedByButNotSummed()
    {
        Grouped(new DataQueryRequest { GroupBy = ["state"] }, 5).IsSuccess.ShouldBeTrue();
        Grouped(Sum("state", "total"), 5).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AnUngroupedRequestIsNotAGroupedOne()
        => DataQueryGrouping.IsRequested(new DataQueryRequest { Skip = 3 }).ShouldBeFalse();

    private static DataQueryRequest Sum(string field, string name, IReadOnlyList<string>? groupBy = null) => new()
    {
        GroupBy = groupBy,
        Aggregates = [new DataQueryAggregate { Function = "Sum", Field = field, As = name }],
    };

    private static Fdw.Results.IGenericResult<GroupedQuery> Grouped(DataQueryRequest request, int take)
        => DataQueryTranslator.Create(
                [
                    Field("grade", 1, DatumTypes.Categorical),
                    Field("district", 2, DatumTypes.Categorical),
                    Field("absences", 3, DatumTypes.Measure, "Int64"),
                    Field("note", 4, DatumTypes.Descriptive),
                    Field("state", 5, DatumTypes.Place),
                    new DataSetFieldConfiguration { Name = "margin", Ordinal = 6, TypeName = "Decimal", CalculationName = "Margin", DatumTypeId = DatumTypes.Measure.Id },
                ],
                "Attendance", NullLogger.Instance)
            .Value!.TranslateGrouped(request, take);

    private static DataSetFieldConfiguration Field(string name, int ordinal, IDatumType datum, string typeName = "String")
        => new() { Name = name, Ordinal = ordinal, TypeName = typeName, DatumTypeId = datum.Id };

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
