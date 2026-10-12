using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Web.RestEndpoints.Tests.Query;

/// <summary>
/// FDW-978: the one translation a data set's rows and a resource's list share turns a body into exactly
/// the query asked for -- against the property the store knows the field by -- or refuses it whole.
/// </summary>
public sealed class QueryTranslatorTests
{
    [Fact]
    public void AConditionIsBuiltAgainstTheColumnNotTheWireName()
    {
        var domain = Translator().ToDomainQuery(new ResourceQueryRequest
        {
            Filter = new QueryFilterNode { Field = "isactive", Operator = "Equal", Value = Json("true") },
        }, 25);

        domain.IsSuccess.ShouldBeTrue();
        var condition = domain.Value!.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("IsActive");
        condition.Value.ShouldBe(true);
    }

    [Fact]
    public void ASortIsBuiltAgainstTheColumnToo()
    {
        var domain = Translator().ToDomainQuery(new ResourceQueryRequest
        {
            Sort = [new QuerySort { Field = "createdAt", Direction = "Descending" }],
        }, 25);

        domain.IsSuccess.ShouldBeTrue();
        var ordered = domain.Value!.Ordering!.OrderedFields.ShouldHaveSingleItem();
        ordered.PropertyName.ShouldBe("CreatedAt");
        ordered.Direction.ShouldBe(SortDirections.ByName("Descending"));
    }

    [Fact]
    public void TheDomainQueryCarriesSkipTakeAndWhetherATotalWasAskedFor()
    {
        var domain = Translator().ToDomainQuery(new ResourceQueryRequest { Skip = 40, IncludeTotal = true }, 25);

        domain.IsSuccess.ShouldBeTrue();
        domain.Value!.Skip.ShouldBe(40);
        domain.Value.Take.ShouldBe(25);
        domain.Value.IncludeTotal.ShouldBeTrue();
        domain.Value.Filter.ShouldBeNull();
        domain.Value.Ordering.ShouldBeNull();
    }

    [Fact]
    public void ATotalIsNotAskedForUnlessTheBodySaysSo()
        => Translator().ToDomainQuery(new ResourceQueryRequest(), 25).Value!.IncludeTotal.ShouldBeFalse();

    [Fact]
    public void TheGatewayCommandReadsOneRowPastThePage()
    {
        var command = Translator().Translate(new QueryRequest { Skip = 20 }, 10);

        command.IsSuccess.ShouldBeTrue();
        command.Value!.Paging!.Skip.ShouldBe(20);
        command.Value.Paging.Take.ShouldBe(11);
    }

    [Theory]
    [InlineData("{\"field\":\"password\",\"operator\":\"Equal\",\"value\":1}")]
    [InlineData("{\"field\":\"email\",\"operator\":\"Resembles\",\"value\":1}")]
    [InlineData("{\"field\":\"email\",\"value\":1}")]
    [InlineData("{\"field\":\"email\",\"operator\":\"IsNull\",\"value\":1}")]
    [InlineData("{\"field\":\"email\",\"operator\":\"In\",\"value\":[]}")]
    [InlineData("{\"field\":\"lastLoginAt\",\"operator\":\"IsNull\"}")]
    [InlineData("{\"logic\":\"Xor\",\"nodes\":[{\"field\":\"email\",\"operator\":\"IsNull\"}]}")]
    [InlineData("{\"logic\":\"And\",\"nodes\":[]}")]
    [InlineData("{\"logic\":\"And\",\"nodes\":[{\"field\":\"email\",\"operator\":\"IsNull\"}],\"field\":\"email\"}")]
    public void AFilterThatDoesNotResolveIsRefusedWhole(string filterJson)
    {
        var request = new ResourceQueryRequest { Filter = JsonSerializer.Deserialize<QueryFilterNode>(filterJson, WebJson) };

        var refusals = new Refusals();
        var domain = Translator(refusals).ToDomainQuery(request, 10);

        domain.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem();
    }

    // FDW-1125: the body a generated example used to offer -- a group with no logic and a null node -- is refused naming
    // the member to fix, not with an empty logic the caller never wrote.
    [Theory]
    [InlineData("{\"logic\":null,\"nodes\":[null],\"field\":null,\"operator\":null,\"value\":null}", "filter.logic is missing")]
    [InlineData("{\"logic\":\"And\",\"nodes\":[null]}", "filter.nodes[0] is null")]
    [InlineData("{\"logic\":\"Or\",\"nodes\":[{\"logic\":\"And\",\"nodes\":[]}]}", "filter.nodes[0].nodes must contain at least one node")]
    [InlineData("{\"logic\":\"Xor\",\"nodes\":[{\"field\":\"email\",\"operator\":\"IsNull\"}]}", "filter.logic 'Xor' is not And or Or")]
    [InlineData("{}", "filter must be either a group")]
    public void AFilterNodeOfTheWrongShapeIsRefusedNamingItsPathInTheBody(string filterJson, string reason)
    {
        var refusals = new Refusals();
        var domain = Translator(refusals).ToDomainQuery(
            new ResourceQueryRequest { Filter = JsonSerializer.Deserialize<QueryFilterNode>(filterJson, WebJson) }, 10);

        domain.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem().ShouldContain(reason);
    }

    // FDW-1125: null for any member of the body is the member left out; it never fails deserialization.
    [Fact]
    public void ABodyWhoseMembersAreAllNullReadsAsOneThatLeftThemOut()
    {
        var request = JsonSerializer.Deserialize<ResourceQueryRequest>(
            "{\"filter\":null,\"sort\":null,\"skip\":null,\"take\":null}", WebJson)!;

        request.Skip.ShouldBe(0);
        request.Take.ShouldBeNull();
        request.Filter.ShouldBeNull();
        request.Sort.ShouldBeNull();
        Translator().ToDomainQuery(request, 10).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ASkipThatIsGivenIsStillReadAndOneThatIsNotANumberIsStillRefused()
    {
        JsonSerializer.Deserialize<ResourceQueryRequest>("{\"skip\":30}", WebJson)!.Skip.ShouldBe(30);
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ResourceQueryRequest>("{\"skip\":\"many\"}", WebJson));
    }

    [Fact]
    public void ASortOnAnUnknownFieldOrADirectionThatIsNotOneIsRefused()
    {
        Translator().ToDomainQuery(new ResourceQueryRequest { Sort = [new QuerySort { Field = "nope", Direction = "Ascending" }] }, 10)
            .IsFailure.ShouldBeTrue();
        Translator().ToDomainQuery(new ResourceQueryRequest { Sort = [new QuerySort { Field = "email", Direction = "Sideways" }] }, 10)
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ANegativeSkipIsRefused()
        => Translator().ToDomainQuery(new ResourceQueryRequest { Skip = -1 }, 10).IsFailure.ShouldBeTrue();

    [Fact]
    public void ARefusalNamesTheSubjectTheFieldsBelongTo()
    {
        var refusals = new Refusals();
        Translator(refusals).ToDomainQuery(new ResourceQueryRequest
        {
            Filter = new QueryFilterNode { Field = "nope", Operator = "Equal", Value = Json("1") },
        }, 10);

        refusals.Reasons.ShouldHaveSingleItem().ShouldContain("users");
    }

    [Fact]
    public void FieldsThatDifferOnlyByCaseAreRefused()
        => QueryTranslator.Create(
                [new QueryableField("email", "Email", typeof(string)), new QueryableField("EMAIL", "Email2", typeof(string))], "users", new Refusals())
            .IsFailure.ShouldBeTrue();

    // ── FDW-997: a filter value is checked against the field's declared type before any command is built ──

    private static IGenericResult<DomainQuery> Filtered(string field, string op, string valueJson, Refusals? refusals = null)
        => Translator(refusals).ToDomainQuery(new ResourceQueryRequest
        {
            Filter = new QueryFilterNode { Field = field, Operator = op, Value = Json(valueJson) },
        }, 10);

    private static object? ValueOf(IGenericResult<DomainQuery> query)
        => query.Value!.Filter!.Root.ShouldBeOfType<FilterCondition>().Value;

    [Fact]
    public void AGuidFieldTakesAGuidAsTypedValue()
    {
        var id = Guid.NewGuid();

        var query = Filtered("id", "Equal", "\"" + id + "\"");

        query.IsSuccess.ShouldBeTrue();
        ValueOf(query).ShouldBe(id);
    }

    [Theory]
    [InlineData("\"not-a-guid\"")]
    [InlineData("\"\"")]
    [InlineData("12")]
    [InlineData("true")]
    public void AMalformedGuidIsRefusedNamingTheFieldAndNeverReachesACommand(string valueJson)
    {
        var refusals = new Refusals();

        var query = Filtered("id", "Equal", valueJson, refusals);

        query.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem().ShouldContain("'id'");
    }

    [Fact]
    public void ABooleanFieldTakesTrueOrFalseAsJsonOrAsText()
    {
        ValueOf(Filtered("isActive", "Equal", "true")).ShouldBe(true);
        ValueOf(Filtered("isActive", "Equal", "\"false\"")).ShouldBe(false);
        Filtered("isActive", "Equal", "\"yes\"").IsFailure.ShouldBeTrue();
        Filtered("isActive", "Equal", "1").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AWholeNumberFieldTakesANumberOrNumericTextAndIsTypedToTheField()
    {
        ValueOf(Filtered("units", "Equal", "5")).ShouldBe(5);
        ValueOf(Filtered("units", "Equal", "\"7\"")).ShouldBe(7);
        ValueOf(Filtered("rank", "Equal", "9000000000")).ShouldBe(9000000000L);
    }

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("1.5")]
    [InlineData("true")]
    [InlineData("3000000000")]
    public void AValueThatIsNotAWholeNumberOfTheFieldsRangeIsRefused(string valueJson)
        => Filtered("units", "Equal", valueJson).IsFailure.ShouldBeTrue();

    [Fact]
    public void ADecimalAndADoubleFieldTakeNumbersAndNumericText()
    {
        ValueOf(Filtered("amount", "GreaterThan", "12.50")).ShouldBe(12.50m);
        ValueOf(Filtered("amount", "GreaterThan", "\"3\"")).ShouldBe(3m);
        ValueOf(Filtered("weight", "LessThan", "0.25")).ShouldBe(0.25d);
        Filtered("amount", "Equal", "\"abc\"").IsFailure.ShouldBeTrue();
        Filtered("weight", "Equal", "false").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ADateFieldTakesIso8601TextAndReadsAValueWithoutAnOffsetAsUtc()
    {
        ValueOf(Filtered("createdAt", "GreaterThan", "\"2026-09-29T21:00:00+00:00\""))
            .ShouldBe(new DateTimeOffset(2026, 9, 29, 21, 0, 0, TimeSpan.Zero));
        ValueOf(Filtered("createdAt", "GreaterThan", "\"2026-09-29T21:00:00\""))
            .ShouldBe(new DateTimeOffset(2026, 9, 29, 21, 0, 0, TimeSpan.Zero));
        ValueOf(Filtered("createdAt", "LessThan", "\"2026-09-29T23:00:00+02:00\""))
            .ShouldBe(new DateTimeOffset(2026, 9, 29, 21, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("\"yesterday\"")]
    [InlineData("\"2026-13-40\"")]
    [InlineData("20260929")]
    [InlineData("true")]
    public void ADateThatDoesNotParseIsRefused(string valueJson)
        => Filtered("createdAt", "GreaterThan", valueJson).IsFailure.ShouldBeTrue();

    [Fact]
    public void AnInListIsCheckedElementByElementAndOneBadElementRefusesTheWholeQuery()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var good = Filtered("id", "In", "[\"" + a + "\",\"" + b + "\"]");
        var bad = Filtered("id", "In", "[\"" + a + "\",\"nope\"]");

        good.IsSuccess.ShouldBeTrue();
        ValueOf(good).ShouldBeOfType<List<object?>>().ShouldBe(new List<object?> { a, b });
        bad.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ATextPatternOperatorTakesTextWhateverTypeTheFieldHolds()
    {
        // Contains on a Guid field is a match on its text, so "abc" is a pattern and not a malformed GUID.
        var query = Filtered("id", "Contains", "\"abc\"");

        query.IsSuccess.ShouldBeTrue();
        ValueOf(query).ShouldBe("abc");
        ValueOf(Filtered("units", "StartsWith", "25")).ShouldBe(25L);
    }

    [Fact]
    public void AStringFieldTakesTextAndReadsANumberOrABooleanAsItsText()
    {
        ValueOf(Filtered("email", "Equal", "\"a@b.c\"")).ShouldBe("a@b.c");
        ValueOf(Filtered("email", "Equal", "42")).ShouldBe("42");
        ValueOf(Filtered("email", "Equal", "true")).ShouldBe("true");
    }

    [Fact]
    public void AFieldThatDeclaresNoTypePassesTheValueAsTheJsonGaveIt()
    {
        ValueOf(Filtered("untyped", "Equal", "5")).ShouldBe(5L);
        ValueOf(Filtered("untyped", "Equal", "\"x\"")).ShouldBe("x");
    }

    [Fact]
    public void AnArrayOrObjectWhereAScalarIsExpectedIsStillRefused()
    {
        Filtered("email", "Equal", "{\"a\":1}").IsFailure.ShouldBeTrue();
        Filtered("id", "Equal", "[1]").IsFailure.ShouldBeTrue();
    }

    // FDW-1044: Between and NotIn are read into operators every connection already renders.

    private static (string Operator, object? Value)[] Leaves(IGenericResult<DomainQuery> query)
        => query.Value!.Filter!.Root.ShouldBeOfType<FilterGroup>().Nodes
            .Select(n => n.ShouldBeOfType<FilterCondition>())
            .Select(c => (c.Operator.Name, c.Value))
            .ToArray();

    [Fact]
    public void ANumberBetweenIsAnInclusiveRangeOfStartAndStop()
    {
        var query = Filtered("units", "Between", "{\"start\":1,\"stop\":5}");

        query.IsSuccess.ShouldBeTrue();
        Leaves(query).ShouldBe(new (string, object?)[] { ("GreaterThanOrEqual", 1), ("LessThanOrEqual", 5) });
    }

    [Fact]
    public void ADecimalBetweenTakesTheSameStartAndStop()
    {
        var query = Filtered("amount", "Between", "{\"start\":\"1.5\",\"stop\":2}");

        query.IsSuccess.ShouldBeTrue();
        Leaves(query).ShouldBe(new (string, object?)[] { ("GreaterThanOrEqual", 1.5m), ("LessThanOrEqual", 2m) });
    }

    private static IGenericResult<DomainQuery> DateRange(string field, string? startJson, string? endJson, string? op = null, string? valueJson = null)
        => Translator().ToDomainQuery(new ResourceQueryRequest
        {
            Filter = new QueryFilterNode
            {
                Field = field,
                Operator = op,
                Value = valueJson is null ? null : Json(valueJson),
                StartDate = startJson is null ? null : Json(startJson),
                EndDate = endJson is null ? null : Json(endJson),
            },
        }, 10);

    [Fact]
    public void ADateRangeTakesStartDateAndEndDateAndAnEndMomentIsInclusive()
    {
        var query = DateRange("createdAt", "\"2026-01-01\"", "\"2026-01-31T12:00:00\"");

        query.IsSuccess.ShouldBeTrue();
        Leaves(query).ShouldBe(new (string, object?)[]
        {
            ("GreaterThanOrEqual", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            ("LessThanOrEqual", new DateTimeOffset(2026, 1, 31, 12, 0, 0, TimeSpan.Zero)),
        });
    }

    [Fact]
    public void ADateOnlyEndDateKeepsTheWholeOfThatDay()
    {
        var query = DateRange("createdAt", "\"2026-01-01\"", "\"2026-01-31\"");

        query.IsSuccess.ShouldBeTrue();
        Leaves(query).ShouldBe(new (string, object?)[]
        {
            ("GreaterThanOrEqual", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            ("LessThan", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)),
        });
    }

    [Fact]
    public void ADateRangeMayNameOnlyOneEnd()
    {
        var from = DateRange("createdAt", "\"2026-01-01\"", null);
        var until = DateRange("createdAt", null, "\"2026-01-31\"");

        from.IsSuccess.ShouldBeTrue();
        var open = from.Value!.Filter!.Root.ShouldBeOfType<FilterCondition>();
        open.Operator.Name.ShouldBe("GreaterThanOrEqual");
        open.Value.ShouldBe(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        until.IsSuccess.ShouldBeTrue();
        var closed = until.Value!.Filter!.Root.ShouldBeOfType<FilterCondition>();
        closed.Operator.Name.ShouldBe("LessThan");
        closed.Value.ShouldBe(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ADateRangeBoundIsReadLikeAnyOtherDateValue()
        => DateRange("createdAt", "\"2026-01-01T00:00:00+02:00\"", null).Value!.Filter!.Root
            .ShouldBeOfType<FilterCondition>().Value
            .ShouldBe(new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("units", "1", "5", null, null)]                                  // a number field takes Between, not a date range
    [InlineData("email", "\"a\"", "\"z\"", null, null)]
    [InlineData("id", "\"2026-01-01\"", null, null, null)]
    [InlineData("untyped", "\"2026-01-01\"", null, null, null)]
    [InlineData("createdAt", "\"2026-02-01\"", "\"2026-01-01\"", null, null)]     // startDate after endDate
    [InlineData("createdAt", "\"yesterday\"", null, null, null)]
    [InlineData("createdAt", null, "20260131", null, null)]
    [InlineData("createdAt", "\"2026-01-01\"", null, "GreaterThan", "\"2026-01-01\"")] // a range and an operator are not both
    [InlineData("createdAt", "\"2026-01-01\"", null, null, "\"2026-01-01\"")]
    public void ADateRangeThatIsNotOnADateFieldOrNotARangeIsRefused(
        string field, string? startJson, string? endJson, string? op, string? valueJson)
        => DateRange(field, startJson, endJson, op, valueJson).IsFailure.ShouldBeTrue();

    [Fact]
    public void ADateFieldTakesNoBetweenOperator()
        => Filtered("createdAt", "Between", "{\"startDate\":\"2026-01-01\",\"endDate\":\"2026-01-31\"}").IsFailure.ShouldBeTrue();

    [Fact]
    public void ACalculatedDateFieldCannotBeRanged()
        => DateRange("lastLoginAt", "\"2026-01-01\"", null).IsFailure.ShouldBeTrue();

    [Fact]
    public void ABoundsNamesAreMatchedIgnoringCaseAndEitherOrder()
        => Leaves(Filtered("units", "between", "{\"STOP\":9,\"Start\":3}"))
            .ShouldBe(new (string, object?)[] { ("GreaterThanOrEqual", 3), ("LessThanOrEqual", 9) });

    [Theory]
    [InlineData("email", "{\"start\":\"a\",\"stop\":\"z\"}")]                       // a string field is not a range field
    [InlineData("id", "{\"start\":1,\"stop\":2}")]
    [InlineData("isActive", "{\"start\":false,\"stop\":true}")]
    [InlineData("untyped", "{\"start\":1,\"stop\":2}")]
    [InlineData("units", "{\"startDate\":1,\"endDate\":2}")]                           // a number uses start and stop
    [InlineData("createdAt", "{\"start\":\"2026-01-01\",\"stop\":\"2026-01-31\"}")] // a date uses startDate and endDate
    [InlineData("units", "{\"start\":1}")]
    [InlineData("units", "{\"stop\":1}")]
    [InlineData("units", "{\"start\":1,\"stop\":2,\"step\":1}")]
    [InlineData("units", "{\"start\":5,\"stop\":1}")]                                   // start after stop
    [InlineData("createdAt", "{\"startDate\":\"2026-02-01\",\"endDate\":\"2026-01-01\"}")]
    [InlineData("units", "[1,5]")]
    [InlineData("units", "5")]
    [InlineData("units", "{\"start\":\"one\",\"stop\":5}")]
    [InlineData("createdAt", "{\"startDate\":\"yesterday\",\"endDate\":\"2026-01-31\"}")]
    public void ABetweenThatIsNotARangeOfTheFieldsTypeIsRefused(string field, string valueJson)
        => Filtered(field, "Between", valueJson).IsFailure.ShouldBeTrue();

    [Fact]
    public void ABetweenWithNoValueIsRefused()
        => Translator().ToDomainQuery(new ResourceQueryRequest
        {
            Filter = new QueryFilterNode { Field = "units", Operator = "Between" },
        }, 10).IsFailure.ShouldBeTrue();

    [Fact]
    public void ANotInOnStringsIsEveryValueExcluded()
    {
        var query = Filtered("email", "NotIn", "[\"a@b.c\",\"d@e.f\"]");

        query.IsSuccess.ShouldBeTrue();
        Leaves(query).ShouldBe(new (string, object?)[] { ("NotEqual", "a@b.c"), ("NotEqual", "d@e.f") });
    }

    [Fact]
    public void ANotInOnIntegersIsEveryValueExcludedAndOneValueIsOneCondition()
    {
        Leaves(Filtered("units", "NotIn", "[1,2,3]"))
            .ShouldBe(new (string, object?)[] { ("NotEqual", 1), ("NotEqual", 2), ("NotEqual", 3) });

        var single = Filtered("rank", "NotIn", "[7]");
        single.IsSuccess.ShouldBeTrue();
        var only = single.Value!.Filter!.Root.ShouldBeOfType<FilterCondition>();
        only.Operator.Name.ShouldBe("NotEqual");
        only.Value.ShouldBe(7L);
    }

    [Theory]
    [InlineData("id", "[\"3f2504e0-4f89-11d3-9a0c-0305e82c3301\"]")]  // NotIn is for strings and whole numbers
    [InlineData("isActive", "[true]")]
    [InlineData("amount", "[1.5]")]
    [InlineData("weight", "[1.5]")]
    [InlineData("createdAt", "[\"2026-01-01\"]")]
    [InlineData("untyped", "[1]")]
    [InlineData("email", "[]")]
    [InlineData("email", "\"a@b.c\"")]
    [InlineData("email", "[\"a@b.c\",null]")]                            // <> null matches nothing, so it is refused
    [InlineData("units", "[1,\"two\"]")]                                // one bad element refuses the whole list
    public void ANotInThatIsNotAListOfStringsOrWholeNumbersIsRefused(string field, string valueJson)
        => Filtered(field, "NotIn", valueJson).IsFailure.ShouldBeTrue();

    [Fact]
    public void ANotInListLongerThanTheCapIsRefusedLikeAnInList()
    {
        var values = "[" + string.Join(",", System.Linq.Enumerable.Range(1, 1001)) + "]";

        Filtered("units", "NotIn", values).IsFailure.ShouldBeTrue();
        Filtered("units", "In", values).IsFailure.ShouldBeTrue();
    }

    // FDW-1045: a calculated field may be named; the part that names one runs after the read.

    [Fact]
    public void AQueryThatNamesNoCalculatedFieldPlansAsThePlainStoreCommand()
    {
        var plan = Plan(new QueryRequest { Filter = Leaf("units", "Equal", "3"), Skip = 20 }).Value!;

        plan.RunsInMemory.ShouldBeFalse();
        plan.Command.Filter!.Root.ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("Units");
        plan.Command.Paging!.Skip.ShouldBe(20);
        plan.Command.Paging.Take.ShouldBe(11);
    }

    [Fact]
    public void ACalculatedFilterRunsAfterTheReadAndTheStoreIsAskedForEveryRowUpToTheCap()
    {
        var plan = Plan(new QueryRequest { Filter = Leaf("score", "GreaterThanOrEqual", "5"), Skip = 20 }, take: 10, cap: 500).Value!;

        plan.RunsInMemory.ShouldBeTrue();
        plan.Command.Filter.ShouldBeNull();
        plan.InMemoryFilter.ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("Score");
        plan.Command.Paging!.Skip.ShouldBe(0);
        plan.Command.Paging.Take.ShouldBe(501);
        plan.Skip.ShouldBe(20);
        plan.Take.ShouldBe(10);
        plan.RowCap.ShouldBe(500);
    }

    [Fact]
    public void ATopLevelAndSendsTheConjunctsThatNameNoCalculatedFieldToTheStore()
    {
        var plan = Plan(new QueryRequest
        {
            Filter = Group("And", Leaf("units", "GreaterThan", "1"), Leaf("score", "LessThan", "9"), Leaf("email", "StartsWith", "\"a\"")),
        }).Value!;

        var pushed = plan.Command.Filter!.Root.ShouldBeOfType<FilterGroup>().Nodes
            .Select(n => n.ShouldBeOfType<FilterCondition>().PropertyName).ToArray();
        pushed.ShouldBe(new[] { "Units", "Email" });
        plan.InMemoryFilter.ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("Score");
    }

    [Fact]
    public void AnOrThatNamesACalculatedFieldRunsEntirelyAfterTheRead()
    {
        var plan = Plan(new QueryRequest { Filter = Group("Or", Leaf("units", "Equal", "1"), Leaf("score", "Equal", "2")) }).Value!;

        plan.Command.Filter.ShouldBeNull();
        plan.InMemoryFilter.ShouldBeOfType<FilterGroup>().Nodes.Count.ShouldBe(2);
    }

    [Fact]
    public void AnOrNestedInAnAndKeepsItsOwnSideTogether()
    {
        var plan = Plan(new QueryRequest
        {
            Filter = Group("And", Group("Or", Leaf("units", "Equal", "1"), Leaf("score", "Equal", "2")), Leaf("email", "Equal", "\"a\"")),
        }).Value!;

        plan.Command.Filter!.Root.ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("Email");
        plan.InMemoryFilter.ShouldBeOfType<FilterGroup>().Operator.ShouldBe(LogicalOperator.Or);
    }

    [Fact]
    public void ASortOnACalculatedFieldRunsAfterTheReadAndLeavesTheStoreUnordered()
    {
        var plan = Plan(new QueryRequest { Sort = [new QuerySort { Field = "score", Direction = "Descending" }] }).Value!;

        plan.RunsInMemory.ShouldBeTrue();
        plan.Command.Ordering.ShouldBeNull();
        plan.InMemoryOrdering!.OrderedFields.ShouldHaveSingleItem().PropertyName.ShouldBe("Score");
        plan.InMemoryFilter.ShouldBeNull();
    }

    [Fact]
    public void ASortOnARealFieldStaysInTheStoreEvenWhenTheFilterRunsAfterTheRead()
    {
        var plan = Plan(new QueryRequest
        {
            Filter = Leaf("score", "GreaterThan", "1"),
            Sort = [new QuerySort { Field = "units", Direction = "Ascending" }],
        }).Value!;

        plan.Command.Ordering!.OrderedFields.ShouldHaveSingleItem().PropertyName.ShouldBe("Units");
        plan.InMemoryOrdering.ShouldBeNull();
        plan.InMemoryFilter.ShouldNotBeNull();
    }

    [Fact]
    public void ADateRangeOnACalculatedDateFieldIsPlannedLikeAnyOtherCalculatedFilter()
    {
        var plan = Plan(new QueryRequest { Filter = new QueryFilterNode { Field = "lastLoginAt", StartDate = Json("\"2026-01-01\"") } }).Value!;

        plan.InMemoryFilter.ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("LastLoginAt");
        plan.Command.Filter.ShouldBeNull();
    }

    [Fact]
    public void TheTranslatorThatIsNotPlanningStillRefusesACalculatedFieldInAFilterOrASort()
    {
        Filtered("score", "Equal", "1").IsFailure.ShouldBeTrue();
        Translator().ToDomainQuery(new ResourceQueryRequest { Sort = [new QuerySort { Field = "score", Direction = "Ascending" }] }, 10)
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ARefusedQueryIsStillRefusedWhenPlanning()
        => Plan(new QueryRequest { Filter = Leaf("nosuchfield", "Equal", "1") }).IsFailure.ShouldBeTrue();

    private static QueryableField[] Fields() =>
    [
        new QueryableField("id", "Id", typeof(Guid)),
        new QueryableField("email", "Email", typeof(string)),
        new QueryableField("isActive", "IsActive", typeof(bool)),
        new QueryableField("createdAt", "CreatedAt", typeof(DateTimeOffset)),
        new QueryableField("units", "Units", typeof(int)),
        new QueryableField("amount", "Amount", typeof(decimal)),
        new QueryableField("weight", "Weight", typeof(double)),
        new QueryableField("rank", "Rank", typeof(long)),
        new QueryableField("untyped", "Untyped", null),
        new QueryableField("lastLoginAt", "LastLoginAt", typeof(DateTimeOffset), IsCalculated: true),
        new QueryableField("score", "Score", typeof(int), IsCalculated: true),
    ];

    private static QueryTranslator Translator(Refusals? refusals = null)
        => QueryTranslator.Create(Fields(), "users", refusals ?? new Refusals()).Value!;

    // The translator a data route plans with: a calculated field may be named, and the part that names one runs after the read.
    private static QueryTranslator Planner()
        => QueryTranslator.Create(Fields(), "users", new Refusals(), allowCalculated: true).Value!;

    private static IGenericResult<QueryPlan> Plan(QueryRequest request, int take = 10, int cap = 500)
        => Planner().Plan(request, take, cap);

    private static QueryFilterNode Leaf(string field, string op, string valueJson)
        => new() { Field = field, Operator = op, Value = Json(valueJson) };

    private static QueryFilterNode Group(string logic, params QueryFilterNode[] nodes)
        => new() { Logic = logic, Nodes = nodes };

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class Refusals : IQueryRefusals
    {
        public List<string> Reasons { get; } = [];

        public IGenericResult<T> Invalid<T>(string reason)
        {
            Reasons.Add(reason);
            return GenericResult<T>.Failure(new GenericMessage(reason));
        }
    }
}
