using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for a for each that works through its list in batches (FDW-3086): a round takes that many items in order, its variable holds them as a
/// list, the last round may take fewer, and rounds go one after another.
/// </summary>
public sealed class FlowForEachBatchTests
{
    private static readonly string[] Names = ["a", "b", "c", "d", "e"];

    private static FlowStepContext Context()
    {
        var context = new FlowStepContext(new FlowVariables(), new FlowExecutor(), Options(), null, null);
        context.Variables.Declare("Names", Names);
        return context;
    }

    private static ScriptedStep Step(string name, Func<FlowStepContext, Task<IGenericResult>> run) => new(name, run);

    private static Task<IGenericResult> Success() => Task.FromResult<IGenericResult>(GenericResult.Success());

    // The step that writes down what the round's variable holds.
    private static ScriptedStep Record(List<string> seen) => Step("record", context =>
    {
        context.Variables.TryGet("Batch", out var held);
        seen.Add(held is IEnumerable<object?> batch ? string.Join(",", batch) : $"item {held}");
        return Success();
    });

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(2, "a,b|c,d|e")]
    [InlineData(1, "a|b|c|d|e")]
    [InlineData(5, "a,b,c,d,e")]
    [InlineData(9, "a,b,c,d,e")]
    public async Task ARoundTakesTheBatchSizeInOrderAndTheLastRoundMayTakeFewer(int batchSize, string expected)
    {
        var seen = new List<string>();

        var result = await new FlowForEach("each", "Batch", "Names", [Record(seen)], batchSize).Run(Context(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        string.Join("|", seen).ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WithoutABatchSizeARoundTakesOneItemAndTheVariableIsTheItemNotAList()
    {
        var seen = new List<string>();

        var result = await new FlowForEach("each", "Batch", "Names", [Record(seen)]).Run(Context(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        seen.ShouldBe(["item a", "item b", "item c", "item d", "item e"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnEmptyListGivesNoRoundWhateverTheBatchSize()
    {
        var context = Context();
        context.Variables.Declare("None", Array.Empty<string>());
        var seen = new List<string>();

        var result = await new FlowForEach("each", "Batch", "None", [Record(seen)], 3).Run(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        seen.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(0)]
    [InlineData(-1)]
    public void ABatchSizeBelowOneIsRefusedAtConstruction(int batchSize) =>
        Should.Throw<ArgumentOutOfRangeException>(() => new FlowForEach("each", "Batch", "Names", [Step("n", _ => Success())], batchSize));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RoundsGoOneAfterAnotherAndARoundThatFailsStopsTheLoop()
    {
        var order = new List<string>();
        var round = 0;
        var step = Step("round", async _ =>
        {
            var mine = ++round;
            order.Add($"start {mine}");
            await Task.Delay(15, TestContext.Current.CancellationToken);
            order.Add($"end {mine}");
            return mine == 2 ? GenericResult.Failure(new GenericMessage("round two failed")) : GenericResult.Success();
        });

        var result = await new FlowForEach("each", "Batch", "Names", [step], 2).Run(Context(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        order.ShouldBe(["start 1", "end 1", "start 2", "end 2"]);
    }
}
