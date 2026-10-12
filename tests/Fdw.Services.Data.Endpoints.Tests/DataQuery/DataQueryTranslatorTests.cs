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
/// FDW-876/FDW-975: the body of POST /data/{dataset} (and of a workspace over it) becomes exactly the query asked for, or is
/// refused whole — never a query with a piece silently missing.
/// </summary>
public sealed class DataQueryTranslatorTests
{
    [Fact]
    public void NegativeSkipIsRefused()
        => Translator().Translate(new DataQueryRequest { Skip = -1 }, 10).IsFailure.ShouldBeTrue();

    [Fact]
    public void PagingReadsOneRowPastThePage()
    {
        var command = Translator().Translate(new DataQueryRequest { Skip = 20 }, 10);

        command.IsSuccess.ShouldBeTrue();
        command.Value!.Paging!.Skip.ShouldBe(20);
        command.Value.Paging.Take.ShouldBe(11);
        command.Value.Filter.ShouldBeNull();
    }

    [Fact]
    public void AConditionBecomesAFilterConditionOnTheFieldsOwnName()
    {
        var command = Translator().Translate(new DataQueryRequest
        {
            Filter = new QueryFilterNode { Field = "region", Operator = "Equal", Value = Json("\"West\"") },
        }, 10);

        command.IsSuccess.ShouldBeTrue();
        var condition = command.Value!.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Region");
        condition.Value.ShouldBe("West");
    }

    [Fact]
    public void AGroupWithInCarriesTheArrayAsAList()
    {
        var command = Translator().Translate(new DataQueryRequest
        {
            Filter = new QueryFilterNode
            {
                Logic = "Or",
                Nodes =
                [
                    new QueryFilterNode { Field = "Region", Operator = "In", Value = Json("[\"West\",\"East\"]") },
                    new QueryFilterNode { Field = "Units", Operator = "GreaterThan", Value = Json("5") },
                ],
            },
        }, 10);

        command.IsSuccess.ShouldBeTrue();
        var group = command.Value!.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.Count.ShouldBe(2);
        ((FilterCondition)group.Nodes[0]).Value.ShouldBeOfType<List<object?>>().ShouldBe(new List<object?> { "West", "East" });
        ((FilterCondition)group.Nodes[1]).Value.ShouldBe(5L);
    }

    [Theory]
    [InlineData("{\"field\":\"Nope\",\"operator\":\"Equal\",\"value\":1}")]
    [InlineData("{\"field\":\"Region\",\"operator\":\"Resembles\",\"value\":1}")]
    [InlineData("{\"field\":\"Region\",\"value\":1}")]
    [InlineData("{\"field\":\"Region\",\"operator\":\"Equal\"}")]
    [InlineData("{\"field\":\"Region\",\"operator\":\"IsNull\",\"value\":1}")]
    [InlineData("{\"field\":\"Region\",\"operator\":\"In\",\"value\":[]}")]
    [InlineData("{\"field\":\"Region\",\"operator\":\"Equal\",\"value\":{\"a\":1}}")]
    [InlineData("{\"field\":\"Units\",\"operator\":\"Equal\",\"value\":1e30}")]
    [InlineData("{\"logic\":\"Xor\",\"nodes\":[{\"field\":\"Region\",\"operator\":\"IsNull\"}]}")]
    [InlineData("{\"logic\":\"And\",\"nodes\":[]}")]
    [InlineData("{\"logic\":\"And\",\"nodes\":[null]}")]
    [InlineData("{\"logic\":\"And\",\"field\":\"Region\",\"operator\":\"IsNull\"}")]
    [InlineData("{}")]
    public void AFilterThatDoesNotResolveRefusesTheWholeQuery(string filterJson)
    {
        var filter = JsonSerializer.Deserialize<QueryFilterNode>(filterJson, WebJson)!;

        Translator().Translate(new DataQueryRequest { Filter = filter }, 10).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void SortResolvesFieldAndDirection()
    {
        var command = Translator().Translate(new DataQueryRequest
        {
            Sort = [new QuerySort { Field = "units", Direction = "Descending" }],
        }, 10);

        command.IsSuccess.ShouldBeTrue();
        var ordered = command.Value!.Ordering!.OrderedFields.ShouldHaveSingleItem();
        ordered.PropertyName.ShouldBe("Units");
        ordered.Direction.IsAscending.ShouldBeFalse();
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("[{\"field\":\"Nope\",\"direction\":\"Ascending\"}]")]
    [InlineData("[{\"field\":\"Units\",\"direction\":\"Sideways\"}]")]
    [InlineData("[{\"field\":null,\"direction\":\"Ascending\"}]")]
    [InlineData("[{\"field\":\"Units\",\"direction\":null}]")]
    public void ASortThatDoesNotResolveRefusesTheWholeQuery(string sortJson)
    {
        var sort = JsonSerializer.Deserialize<List<QuerySort?>>(sortJson, WebJson)!;

        Translator().Translate(new DataQueryRequest { Sort = sort }, 10).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ACalculatedFieldCannotBeFilteredOrSorted()
    {
        var translator = DataQueryTranslator.Create(
                [Field("Region", 1), Calculated("Margin", 2)], "Sales", NullLogger.Instance)
            .Value!;

        translator.Translate(new DataQueryRequest
        {
            Filter = new QueryFilterNode { Field = "Margin", Operator = "GreaterThan", Value = Json("1") },
        }, 10).IsFailure.ShouldBeTrue();
        translator.Translate(new DataQueryRequest
        {
            Sort = [new QuerySort { Field = "Margin", Direction = "Ascending" }],
        }, 10).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AnInListLongerThanAThousandIsRefused()
    {
        var values = "[" + string.Join(",", Enumerable.Range(0, 1001)) + "]";

        Translator().Translate(new DataQueryRequest
        {
            Filter = new QueryFilterNode { Field = "Units", Operator = "In", Value = Json(values) },
        }, 10).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void FieldsThatDifferOnlyByCaseAreRefused()
        => DataQueryTranslator.Create(
                [Field("Region", 1), Field("REGION", 2)], "Sales", NullLogger.Instance)
            .IsFailure.ShouldBeTrue();

    [Fact]
    public void ColumnsAreEveryFieldInOrdinalOrder()
        => Translator().Columns().Select(c => c.Name).ShouldBe(new[] { "Region", "Units" });

    // ── FDW-997: a value is checked against the type the data set declares for the field ──

    private static IFilterNode? Root(DataQueryTranslator translator, string field, string op, string valueJson)
    {
        var command = translator.Translate(new DataQueryRequest
        {
            Filter = new QueryFilterNode { Field = field, Operator = op, Value = Json(valueJson) },
        }, 10);
        return command.IsSuccess ? command.Value!.Filter!.Root : null;
    }

    [Fact]
    public void AValueOfTheDeclaredTypeIsTypedToIt()
    {
        var translator = DataQueryTranslator.Create(
            [Field("Id", 1, "Guid"), Field("Active", 2, "Bool"), Field("Amount", 3, "Decimal"), Field("On", 4, "DateTimeOffset")],
            "Sales", NullLogger.Instance).Value!;
        var id = System.Guid.NewGuid();

        ((FilterCondition)Root(translator, "Id", "Equal", "\"" + id + "\"")!).Value.ShouldBe(id);
        ((FilterCondition)Root(translator, "Active", "Equal", "true")!).Value.ShouldBe(true);
        ((FilterCondition)Root(translator, "Amount", "GreaterThan", "12.5")!).Value.ShouldBe(12.5m);
        ((FilterCondition)Root(translator, "On", "LessThan", "\"2026-09-29T21:00:00Z\"")!).Value
            .ShouldBe(new System.DateTimeOffset(2026, 9, 29, 21, 0, 0, System.TimeSpan.Zero));
    }

    [Theory]
    [InlineData("Id", "Guid", "\"nope\"")]
    [InlineData("Active", "Bool", "\"maybe\"")]
    [InlineData("Amount", "Decimal", "\"abc\"")]
    [InlineData("On", "DateTimeOffset", "\"not a date\"")]
    [InlineData("Units", "Int32", "\"lots\"")]
    public void AValueThatIsNotOfTheDeclaredTypeIsRefusedAsDataQueryInvalidNamingTheField(string field, string typeName, string valueJson)
    {
        var translator = DataQueryTranslator.Create([Field(field, 1, typeName)], "Sales", NullLogger.Instance).Value!;

        var command = translator.Translate(new DataQueryRequest
        {
            Filter = new QueryFilterNode { Field = field, Operator = "Equal", Value = Json(valueJson) },
        }, 10);

        command.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AFieldWhoseTypeNameIsNotAKnownOneDeclaresNoTypeAndPassesTheValueAsGiven()
    {
        var translator = DataQueryTranslator.Create([Field("Blob", 1, "Some.Custom.Thing")], "Sales", NullLogger.Instance).Value!;

        ((FilterCondition)Root(translator, "Blob", "Equal", "5")!).Value.ShouldBe(5L);
    }

    [Fact]
    public void ANumberForAStringFieldIsItsText()
        => ((FilterCondition)Root(Translator(), "Region", "Equal", "5")!).Value.ShouldBe("5");

    // FDW-1048: an administrator can switch a field off for callers; the check is on the caller's own filter.
    [Fact]
    public void AFilterOnAFieldMadeNotFilterableIsRefused()
    {
        var translator = FilterableTranslator();

        var result = translator.RequireFilterable(new QueryFilterNode { Field = "Secret", Operator = "Equal", Value = Json("\"x\"") });

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ANotFilterableFieldInsideAGroupIsRefusedAndTheRestPass()
    {
        var translator = FilterableTranslator();
        var open = new QueryFilterNode { Field = "Region", Operator = "Equal", Value = Json("\"West\"") };
        var closed = new QueryFilterNode { Field = "secret", Operator = "Equal", Value = Json("\"x\"") };

        translator.RequireFilterable(new QueryFilterNode { Logic = "And", Nodes = [open] }).IsSuccess.ShouldBeTrue();
        translator.RequireFilterable(new QueryFilterNode { Logic = "And", Nodes = [open, new QueryFilterNode { Logic = "Or", Nodes = [closed] }] })
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void NoFilterAndAnUnknownFieldPassTheFilterableCheckSoTheTranslatorRefusesTheUnknownOneByName()
    {
        var translator = FilterableTranslator();

        translator.RequireFilterable(null).IsSuccess.ShouldBeTrue();
        translator.RequireFilterable(new QueryFilterNode { Field = "Nope", Operator = "Equal", Value = Json("1") }).IsSuccess.ShouldBeTrue();
    }

    // FDW-1102: a filterable field takes the operators its datum type and data type allow, and no more.
    [Fact]
    public void AnOperatorTheFieldsTypesDoNotAllowIsRefusedWithTheOnesItTakes()
    {
        var translator = Over(Typed("Region", DatumTypes.Categorical.Id, "String"));

        var result = translator.RequireFilterable(new QueryFilterNode { Field = "Region", Operator = "Contains", Value = Json("\"We\"") });

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("operator 'Contains' cannot be used on 'Region'; it takes Equal, NotEqual, In, NotIn, IsNull, IsNotNull");
    }

    [Fact]
    public void AnOperatorTheFieldTakesPassesTheCheck()
    {
        var translator = Over(Typed("Region", DatumTypes.Categorical.Id, "String"));

        translator.RequireFilterable(new QueryFilterNode { Field = "Region", Operator = "NotIn", Value = Json("[\"a\"]") }).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ADateRangeIsTheBetweenFormAndPassesOnAFieldThatTakesBetween()
    {
        var translator = Over(Typed("At", DatumTypes.Date.Id, "DateTimeOffset"));

        var range = new QueryFilterNode { Field = "At", StartDate = Json("\"2026-01-01\""), EndDate = Json("\"2026-01-31\"") };

        translator.RequireFilterable(range).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ADateRangeIsRefusedOnAFieldWhoseTypesTakeNoBetween()
    {
        var translator = Over(Typed("Code", DatumTypes.Identifier.Id, "String"));

        translator.RequireFilterable(new QueryFilterNode { Field = "Code", StartDate = Json("\"2026-01-01\"") }).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AnUntypedFieldTakesNoOperatorAndTheRefusalNamesTheRouteThatTypesIt()
    {
        var translator = Over(Typed("Region", DatumTypes.NotYetTyped.Id, "String"));

        var result = translator.RequireFilterable(new QueryFilterNode { Field = "Region", Operator = "Equal", Value = Json("\"West\"") });

        result.IsFailure.ShouldBeTrue();
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("'Region' is not yet typed");
        message.ShouldContain("PATCH /datasets/Sales/fields/datum-type");
    }

    [Fact]
    public void AColumnReportsTheOperatorsItsFieldTakesAndNoneWhenNotFilterableOrUntyped()
    {
        var columns = Over(
                Typed("Units", DatumTypes.Measure.Id, "Decimal"),
                Typed("Blank", DatumTypes.NotYetTyped.Id, "String"),
                Typed("Secret", DatumTypes.Attribute.Id, "String", isFilterable: false))
            .Columns();

        var units = columns.Single(c => c.Name == "Units").FilterOperators;
        units.ShouldContain("Between");
        units.ShouldContain("GreaterThanOrEqual");
        units.ShouldNotContain("NotIn");
        columns.Single(c => c.Name == "Blank").FilterOperators.ShouldBeEmpty();
        columns.Single(c => c.Name == "Secret").FilterOperators.ShouldBeEmpty();
    }

    [Fact]
    public void AFieldIsFilterableWhenItIsStatedSoAndTheColumnsSayWhich()
    {
        var translator = FilterableTranslator();

        translator.Columns().Single(c => c.Name == "Region").IsFilterable.ShouldBeTrue();
        translator.Columns().Single(c => c.Name == "Secret").IsFilterable.ShouldBeFalse();
    }

    private static DataQueryTranslator FilterableTranslator()
        => DataQueryTranslator.Create(
                [Field("Region", 1), new DataSetFieldConfiguration { Name = "Secret", Ordinal = 2, TypeName = "String", IsFilterable = false }],
                "Sales", NullLogger.Instance)
            .Value!;

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static DataQueryTranslator Translator()
        => DataQueryTranslator.Create(
                [Field("Units", 2, "Int64"), Field("Region", 1)], "Sales", NullLogger.Instance)
            .Value!;

    // A typed, filterable field: FDW-1102 gives an untyped field no filter operator and a field no default for isFilterable.
    private static DataSetFieldConfiguration Field(string name, int ordinal, string typeName = "String")
        => new() { Name = name, Ordinal = ordinal, TypeName = typeName, IsFilterable = true, DatumTypeId = DatumTypes.Attribute.Id };

    private static DataSetFieldConfiguration Typed(string name, int datumTypeId, string typeName, bool isFilterable = true)
        => new() { Name = name, Ordinal = 1, TypeName = typeName, IsFilterable = isFilterable, DatumTypeId = datumTypeId };

    private static DataQueryTranslator Over(params DataSetFieldConfiguration[] fields)
        => DataQueryTranslator.Create(fields, "Sales", NullLogger.Instance).Value!;

    private static DataSetFieldConfiguration Calculated(string name, int ordinal)
        => new() { Name = name, Ordinal = ordinal, TypeName = "Decimal", CalculationName = "Margin" };

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
