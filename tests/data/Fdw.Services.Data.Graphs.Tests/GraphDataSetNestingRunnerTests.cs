using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>FDW-5299: a Graph data set reads data sets nested in layers through the gateway; a cycle and a nesting past the stated depth are refused by name before anything is read.</summary>
public sealed class GraphDataSetNestingRunnerTests
{
    private static GraphDataSetNestingRunner Runner(NestingGateway world, int maxDepth)
    {
        var runner = new GraphDataSetNestingRunner(
            new GraphDataSetRunner(new GraphDataSetReadOptions(1000, 30, 2), NullLogger<GraphDataSetRunner>.Instance),
            world.Sources,
            new GraphDataSetNestingOptions(maxDepth),
            NullLogger<GraphDataSetNestingRunner>.Instance);
        world.Runner = runner;
        return runner;
    }

    private static async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> ReadTop(NestingGateway world, int maxDepth) =>
        await Runner(world, maxDepth).Read(world.Gateway, NestingGateway.Graph("Top", "Mid"), TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphOverAGraphReadsTwoLevelsThroughTheGateway()
    {
        var world = new NestingGateway().WithGraph("Mid", "Base").WithTable("Base", 5);

        var result = await ReadTop(world, 5);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(row => (int)row["Id"]!).OrderBy(id => id).ShouldBe([0, 1, 2, 3, 4]);
        world.Opened.ShouldBe(["Mid", "Base"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphOverAGraphOverAGraphReadsThreeLevelsThroughTheGateway()
    {
        var world = new NestingGateway().WithGraph("Mid", "Low").WithGraph("Low", "Base").WithTable("Base", 3);

        var result = await ReadTop(world, 3);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(row => (int)row["Id"]!).OrderBy(id => id).ShouldBe([0, 1, 2]);
        world.Opened.ShouldBe(["Mid", "Low", "Base"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ANestingDeeperThanTheStatedDepthIsRefusedByNameAndNothingIsRead()
    {
        var world = new NestingGateway().WithGraph("Mid", "Low").WithGraph("Low", "Base").WithTable("Base", 3);

        var result = await ReadTop(world, 2);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetDepthExceededCode>();
        world.Opened.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ACycleAtReadIsRefusedWithTheChainAndNothingIsRead()
    {
        var world = new NestingGateway().WithGraph("Mid", "Low").WithGraph("Low", "Mid");

        var result = await ReadTop(world, 8);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetCycleCode>();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Mid -> Low -> Mid");
        world.Opened.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphThatIsReadByAGraphItReadsIsRefusedAsACycleBackToTheStart()
    {
        var world = new NestingGateway().WithGraph("Mid", "Top");

        var result = await ReadTop(world, 8);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetCycleCode>();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Top -> Mid -> Top");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ADepthThatIsNotAPositiveNumberRefusesTheReadByNameAndNothingIsRead(int maxDepth)
    {
        var world = new NestingGateway().WithGraph("Mid", "Base").WithTable("Base", 1);

        var result = await ReadTop(world, maxDepth);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetLimitNotStatedCode>();
        world.Opened.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ANestedGraphThatCannotBeReadAnswersTheOuterReadAFailureAndNoRows()
    {
        var world = new NestingGateway().WithGraph("Mid", "Gone");

        var result = await ReadTop(world, 8);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetRunFailedCode>();
    }
}
