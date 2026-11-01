using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Web.RestEndpoints.Tests.Query;

/// <summary>
/// FDW-2333: a filter value may name a calculation. It is evaluated when the query runs, its result is checked against the type of
/// the field, and the translator then reads the same literal the filter would have held.
/// </summary>
public sealed class FilterCalculationTests
{
    private static readonly DateTimeOffset Yesterday = new(2026, 10, 21, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ADateTimeCalculationIsComparedLikeTheLiteralItYields()
    {
        var calculations = new FakeCalculations().Yields("Yesterday", typeof(DateTimeOffset), Yesterday);

        var withCalculation = await Translated(Leaf("createdAt", "GreaterThanOrEqual", "{\"calculation\":\"Yesterday\"}"), calculations);
        var withLiteral = await Translated(Leaf("createdAt", "GreaterThanOrEqual", "\"2026-10-21T05:00:00Z\""), calculations);

        ValueOf(withCalculation).ShouldBe(Yesterday);
        ValueOf(withCalculation).ShouldBe(ValueOf(withLiteral));
    }

    [Fact]
    public async Task ANumberCalculationIsTypedToTheFieldItIsComparedWith()
    {
        var calculations = new FakeCalculations().Yields("Five", typeof(decimal), 5m);

        var filter = await Translated(Leaf("units", "GreaterThan", "{\"calculation\":\"Five\"}"), calculations);

        ValueOf(filter).ShouldBe(5);
    }

    [Fact]
    public async Task TheValuesTheFilterSuppliesReachTheCalculationAsInputs()
    {
        var calculations = new FakeCalculations().Yields("Seconds ago", typeof(DateTimeOffset), Yesterday);

        await Translated(
            Leaf("createdAt", "LessThan", "{\"calculation\":\"Seconds ago\",\"inputs\":{\"seconds\":33,\"flag\":true}}"), calculations);

        var received = calculations.Calls.ShouldHaveSingleItem();
        received.Name.ShouldBe("Seconds ago");
        received.Inputs["seconds"].ShouldBe(33m);
        received.Inputs["flag"].ShouldBe(true);
    }

    [Fact]
    public async Task EitherEndOfADateRangeMayBeACalculation()
    {
        var calculations = new FakeCalculations().Yields("Yesterday", typeof(DateTimeOffset), Yesterday);
        var node = new QueryFilterNode
        {
            Field = "createdAt",
            StartDate = Json("{\"calculation\":\"Yesterday\"}"),
            EndDate = Json("\"2026-10-30T00:00:00Z\""),
        };

        var filter = await Translated(node, calculations);

        var group = filter.Value!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Cast<FilterCondition>().Select(condition => condition.Value).ShouldBe(
            new object[] { Yesterday, new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero) });
    }

    [Fact]
    public async Task TheBoundsOfABetweenMayBeCalculations()
    {
        var calculations = new FakeCalculations().Yields("Low", typeof(decimal), 3m);
        var node = Leaf("amount", "Between", "{\"start\":{\"calculation\":\"Low\"},\"stop\":10}");

        var filter = await Translated(node, calculations);

        var group = filter.Value!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Cast<FilterCondition>().Select(condition => condition.Value).ShouldBe(new object[] { 3m, 10m });
    }

    [Fact]
    public async Task EveryNodeOfAGroupIsResolved()
    {
        var calculations = new FakeCalculations().Yields("Five", typeof(decimal), 5m).Yields("Yesterday", typeof(DateTimeOffset), Yesterday);
        var node = new QueryFilterNode
        {
            Logic = "And",
            Nodes =
            [
                Leaf("units", "Equal", "{\"calculation\":\"Five\"}"),
                new QueryFilterNode { Logic = "Or", Nodes = [Leaf("createdAt", "GreaterThan", "{\"calculation\":\"Yesterday\"}")] },
            ],
        };

        var filter = await Translated(node, calculations);

        filter.IsSuccess.ShouldBeTrue();
        calculations.Calls.Select(call => call.Name).ShouldBe(new[] { "Five", "Yesterday" });
    }

    [Fact]
    public async Task TheFilterThatWasGivenIsNotChanged()
    {
        var calculations = new FakeCalculations().Yields("Five", typeof(decimal), 5m);
        var node = Leaf("units", "Equal", "{\"calculation\":\"Five\"}");

        await Translated(node, calculations);

        FilterCalculationReference.IsReference(node.Value!.Value).ShouldBeTrue();
    }

    [Fact]
    public async Task AResultOfTheWrongTypeForTheFieldIsRefusedNamingBoth()
    {
        var refusals = new Refusals();
        var calculations = new FakeCalculations().Yields("Yesterday", typeof(DateTimeOffset), Yesterday);

        var filter = await Translated(Leaf("amount", "Equal", "{\"calculation\":\"Yesterday\"}"), calculations, refusals);

        filter.IsFailure.ShouldBeTrue();
        var reason = refusals.Reasons.ShouldHaveSingleItem();
        reason.ShouldContain("'Yesterday'");
        reason.ShouldContain("'amount'");
    }

    [Fact]
    public async Task ANumberResultIsRefusedForADateField()
    {
        var refusals = new Refusals();
        var calculations = new FakeCalculations().Yields("Five", typeof(decimal), 5m);

        var filter = await Translated(Leaf("createdAt", "Equal", "{\"calculation\":\"Five\"}"), calculations, refusals);

        filter.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem().ShouldContain("'createdAt'");
    }

    [Fact]
    public async Task AFieldWithNoDeclaredTypeCannotTakeACalculation()
    {
        var refusals = new Refusals();
        var calculations = new FakeCalculations().Yields("Five", typeof(decimal), 5m);

        var filter = await Translated(Leaf("untyped", "Equal", "{\"calculation\":\"Five\"}"), calculations, refusals);

        filter.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem().ShouldContain("'untyped'");
    }

    [Fact]
    public async Task ACalculationThatFailsRefusesTheQueryNamingTheFieldAndTheCalculation()
    {
        var refusals = new Refusals();
        var calculations = new FakeCalculations().Fails("Yesterday", "no clock");

        var filter = await Translated(Leaf("createdAt", "Equal", "{\"calculation\":\"Yesterday\"}"), calculations, refusals);

        filter.IsFailure.ShouldBeTrue();
        var reason = refusals.Reasons.ShouldHaveSingleItem();
        reason.ShouldContain("'Yesterday'");
        reason.ShouldContain("'createdAt'");
        reason.ShouldContain("no clock");
    }

    [Fact]
    public async Task AHostWithNoCalculationsRefusesAFilterThatNamesOne()
    {
        var refusals = new Refusals();

        var filter = await Translated(Leaf("units", "Equal", "{\"calculation\":\"Five\"}"), calculations: null, refusals);

        filter.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem().ShouldContain("'Five'");
    }

    [Theory]
    [InlineData("{\"calculation\":\"Five\",\"inputs\":{\"n\":\"seven\"}}")]
    [InlineData("{\"calculation\":\"Five\",\"inputs\":3}")]
    [InlineData("{\"calculation\":\"Five\",\"extra\":1}")]
    [InlineData("{\"calculation\":\"\"}")]
    [InlineData("{\"calculation\":7}")]
    public async Task AReferenceWithAWrongShapeIsRefused(string reference)
    {
        var refusals = new Refusals();
        var calculations = new FakeCalculations().Yields("Five", typeof(decimal), 5m);

        var filter = await Translated(Leaf("units", "Equal", reference), calculations, refusals);

        filter.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem();
        calculations.Calls.ShouldBeEmpty();
    }

    [Fact]
    public void TheTranslatorRefusesAReferenceThatWasNotResolvedFirst()
    {
        var refusals = new Refusals();

        var filter = Translator(refusals).Filter(Leaf("units", "Equal", "{\"calculation\":\"Five\"}"));

        filter.IsFailure.ShouldBeTrue();
        refusals.Reasons.ShouldHaveSingleItem().ShouldContain("ResolveCalculations");
    }

    [Fact]
    public async Task AFilterWithNoCalculationIsTheSameFilterAsBefore()
    {
        var calculations = new FakeCalculations();

        var filter = await Translated(Leaf("units", "Equal", "5"), calculations);

        ValueOf(filter).ShouldBe(5);
        calculations.Calls.ShouldBeEmpty();
    }

    private static async Task<IGenericResult<IFilterExpression?>> Translated(
        QueryFilterNode node, FakeCalculations? calculations, Refusals? refusals = null)
    {
        var translator = Translator(refusals);
        var resolved = await translator.ResolveCalculations(node, calculations);
        return resolved.IsFailure ? resolved.ToNewResult<IFilterExpression?>() : translator.Filter(resolved.Value);
    }

    private static object? ValueOf(IGenericResult<IFilterExpression?> filter)
        => filter.Value!.Root.ShouldBeOfType<FilterCondition>().Value;

    private static QueryTranslator Translator(Refusals? refusals = null)
        => QueryTranslator.Create(Fields(), "users", refusals ?? new Refusals()).Value!;

    private static QueryableField[] Fields() =>
    [
        new QueryableField("createdAt", "CreatedAt", typeof(DateTimeOffset)),
        new QueryableField("units", "Units", typeof(int)),
        new QueryableField("amount", "Amount", typeof(decimal)),
        new QueryableField("untyped", "Untyped", null),
    ];

    private static QueryFilterNode Leaf(string field, string op, string valueJson)
        => new() { Field = field, Operator = op, Value = Json(valueJson) };

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed record Call(string Name, IReadOnlyDictionary<string, object?> Inputs);

    private sealed class FakeCalculations : IFilterCalculations
    {
        private readonly Dictionary<string, IGenericResult<FilterCalculationValue>> _answers = new(StringComparer.Ordinal);

        public List<Call> Calls { get; } = [];

        public FakeCalculations Yields(string name, Type type, object value)
        {
            _answers[name] = GenericResult<FilterCalculationValue>.Success(new FilterCalculationValue(type, value));
            return this;
        }

        public FakeCalculations Fails(string name, string reason)
        {
            _answers[name] = GenericResult<FilterCalculationValue>.Failure(new GenericMessage(reason));
            return this;
        }

        public Task<IGenericResult<FilterCalculationValue>> Evaluate(
            string calculation, IReadOnlyDictionary<string, object?> inputs, CancellationToken cancellationToken = default)
        {
            Calls.Add(new Call(calculation, inputs));
            return Task.FromResult(_answers[calculation]);
        }
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
