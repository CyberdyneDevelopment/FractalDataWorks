using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the flow components (FDW-1386): the mapping refuses a missing field, the filter expression language is the pipeline's
/// own, and an existing stage kind runs behind input and output nodes.
/// </summary>
public sealed class FlowComponentTests
{
    private readonly FlowExecutor _sut = new();

    private static TransformContext Context() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>());

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMappedFieldTheRowLacksFailsTheRunNamingTheFieldNeverANull()
    {
        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [Source("src", 3), Mapped("map", FlowFieldMap.From("Out", "Missing")), sink],
            [Link("src", "map"), Link("map", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
        rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMappedInputThatStatesNoFieldsOrAFieldTwiceOrIsNotAnInputIsRefusedAtConstruction()
    {
        Should.Throw<ArgumentException>(() => Mapped("map"));
        Should.Throw<ArgumentException>(() => Mapped("map", FlowFieldMap.From("A", "x"), FlowFieldMap.From("a", "y")));
        Should.Throw<ArgumentException>(() => new FlowMappedComponent(
            Pass("map"), new Dictionary<string, IReadOnlyList<FlowFieldMap>> { ["other"] = [FlowFieldMap.From("A", "x")] }));
        Should.Throw<ArgumentException>(() => new FlowMappedComponent(Pass("map"), new Dictionary<string, IReadOnlyList<FlowFieldMap>>()));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AUnionAllOfFewerThanTwoInputsIsRefusedAtConstruction()
    {
        Should.Throw<ArgumentException>(() => new FlowUnionAll("u", ["a"]));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASplitFiltersCanUseTheSameExpressionLanguageAsAStageFilter()
    {
        var filter = FlowPredicate.FromExpression("Id > 3", NullLogger.Instance);
        filter.IsSuccess.ShouldBeTrue(filter.CurrentMessage);
        var (high, highRows) = Sink("high");
        var (low, lowRows) = Sink("low");
        var split = new FlowConditionalSplit(
            "split", [new FlowSplitOutput("high", filter.Value!)], SplitRoutings.ByName("FirstMatch"), FlowUnmatched.SendTo("low"));
        var graph = Graph(
            [Source("src", 6), split, high, low],
            [Link("src", "split"), Link("split", "high", output: "high"), Link("split", "low", output: "low")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(highRows).OrderBy(i => i).ShouldBe(new[] { 4, 5, 6 });
        Ids(lowRows).OrderBy(i => i).ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFilterExpressionThatCannotBeReadIsRefusedWithTheExpression()
    {
        var filter = FlowPredicate.FromExpression("Id > 1 &&", NullLogger.Instance);

        filter.IsFailure.ShouldBeTrue();
        filter.CurrentMessage.ShouldNotBeNull().ShouldContain("Id > 1 &&");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnExistingStreamingStageKindRunsBehindInputAndOutputNodes()
    {
        var stage = new PipelineTransformConfiguration { Name = "Big", OperationType = "Filter", FilterExpression = "Id > 7" };
        var adapter = new FlowStageAdapter("filter", new FilterTransformType(), stage, Context(), batchSize: 4);
        var (sink, rows) = Sink("dst");
        var graph = Graph([Source("src", 10), adapter, sink], [Link("src", "filter"), Link("filter", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(rows).ShouldBe(new[] { 8, 9, 10 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AWholeInputStageKindIsGivenEveryRowOnceWhenTheInputHasEnded()
    {
        var stage = new PipelineTransformConfiguration
        {
            Name = "Order",
            OperationType = "Sort",
            SortFields = [new PipelineTransformSortFieldConfiguration { Name = "Id", FieldName = "Id", Direction = "Descending", Ordinal = 0 }]
        };
        var adapter = new FlowStageAdapter("sort", new SortTransformType(), stage, Context(), batchSize: 4);
        var (sink, rows) = Sink("dst");
        var graph = Graph([Source("src", 10, batchSize: 3), adapter, sink], [Link("src", "sort"), Link("sort", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Ids(rows).ShouldBe(Enumerable.Range(1, 10).Reverse());
    }
}
