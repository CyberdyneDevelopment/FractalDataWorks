using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the control flow of a pipeline (FDW-1403): a barrier between stages, loops with a
/// stated limit, variables in scope and a step that starts another pipeline.
/// </summary>
public sealed class FlowControlTests
{
    private static readonly FlowRunIdentity Identity = new(
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), ["etl-runner"]);

    private static FlowStepContext Context(FakeRunStarter? starter = null, FlowRunIdentity? identity = null) =>
        new(new FlowVariables(), new FlowExecutor(), Options(), starter, identity);

    private static ScriptedStep Step(string name, Func<FlowStepContext, Task<IGenericResult>> run) => new(name, run);

    private static Task<IGenericResult> Done() => Task.FromResult<IGenericResult>(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASecondStageStartsOnlyWhenEveryComponentOfTheFirstHasFinished()
    {
        var context = Context();
        var (slow, kept) = Sink("keep");
        var delayed = new ScriptedComponent("delay", ["input"], ["output"], async (ports, ct) =>
        {
            var input = ports.Input("input");
            var output = ports.Output("output");
            while (await input.Read(ct) is { } batch)
            {
                await Task.Delay(20, ct);
                await output.Write(batch, ct);
            }

            return GenericResult.Success();
        });
        var first = new FlowStageStep(
            "load",
            Graph([Source("src", 12, batchSize: 3), delayed, slow], [Link("src", "delay"), Link("delay", "keep")]));
        List<int> seenBySecondStage = [];
        var second = Step("use", _ =>
        {
            seenBySecondStage.AddRange(Ids(kept));
            return Done();
        });

        var result = await new FlowPipeline("two stages", [first, second]).Run(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        seenBySecondStage.OrderBy(i => i).ShouldBe(Enumerable.Range(1, 12));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailingStageStopsThePipelineBeforeTheNextStage()
    {
        var context = Context();
        var ranSecond = false;
        var failing = new FlowStageStep(
            "bad", Graph([Source("src", 3), new FlowDestination("dst", (_, _) => Task.FromResult<IGenericResult>(
                GenericResult.Failure(new GenericMessage("refused"))))], [Link("src", "dst")]));
        var second = Step("second", _ =>
        {
            ranSecond = true;
            return Done();
        });

        var result = await new FlowPipeline("p", [failing, second]).Run(context, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        ranSecond.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AForLoopRunsItsBodyForEachCounterWithTheCounterInScope()
    {
        var seen = new List<int>();
        var loop = new FlowForLoop("count", "i", start: 1, end: 7, step: 3, [Step("note", c =>
        {
            c.Variables.TryGet("i", out var i);
            seen.Add((int)i!);
            return Done();
        })]);

        var result = await loop.Run(Context(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        seen.ShouldBe(new[] { 1, 4, 7 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AForLoopWithANegativeStepCountsDownAndOneThatMovesAwayRunsNever()
    {
        var down = new List<int>();
        var away = 0;
        var counting = new FlowForLoop("down", "i", 3, 1, -1, [Step("note", c =>
        {
            c.Variables.TryGet("i", out var i);
            down.Add((int)i!);
            return Done();
        })]);
        var wrong = new FlowForLoop("away", "i", 1, 3, -1, [Step("note", _ =>
        {
            away++;
            return Done();
        })]);

        await counting.Run(Context(), TestContext.Current.CancellationToken);
        await wrong.Run(Context(), TestContext.Current.CancellationToken);

        down.ShouldBe(new[] { 3, 2, 1 });
        away.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForLoopWithAStepOfZeroIsRefusedAtConstruction() =>
        Should.Throw<ArgumentException>(() => new FlowForLoop("x", "i", 1, 3, 0, [Step("n", _ => Done())]));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AForEachRunsItsBodyForEachElementOfAListVariableAndRefusesText()
    {
        var context = Context();
        context.Variables.Declare("regions", new[] { "east", "west" });
        context.Variables.Declare("note", "not a list");
        var seen = new List<string>();
        IFlowStep Body() => Step("note", c =>
        {
            c.Variables.TryGet("region", out var region);
            seen.Add((string)region!);
            return Done();
        });

        var good = await new FlowForEach("each", "region", "regions", [Body()]).Run(context, TestContext.Current.CancellationToken);
        var bad = await new FlowForEach("each", "region", "note", [Body()]).Run(context, TestContext.Current.CancellationToken);

        good.IsSuccess.ShouldBeTrue(good.CurrentMessage);
        seen.ShouldBe(new[] { "east", "west" });
        bad.IsFailure.ShouldBeTrue();
        bad.CurrentMessage.ShouldNotBeNull().ShouldContain("does not hold a list");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADoWhileRunsAtLeastOnceAndUntilItsConditionStopsHolding()
    {
        var context = Context();
        context.Variables.Declare("attempt", 0);
        var runs = 0;
        var loop = new FlowDoWhile(
            "retry",
            v => v.TryGet("attempt", out var a) && (int)a! < 3,
            maximum: 10,
            [
                Step("count", _ =>
                {
                    runs++;
                    return Done();
                }),
                new FlowSetVariable("advance", "attempt", v =>
                {
                    v.TryGet("attempt", out var a);
                    return (int)a! + 1;
                })
            ]);

        var result = await loop.Run(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        runs.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADoWhileThatIsStillTrueAtItsLimitFailsNamingTheStepAndTheLimit()
    {
        var loop = new FlowDoWhile("forever", _ => true, maximum: 4, [Step("n", _ => Done())]);

        var result = await loop.Run(Context(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("forever");
        result.CurrentMessage.ShouldContain("4");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task SettingAVariableNobodyDeclaredFailsInsteadOfCreatingIt()
    {
        var result = await new FlowSetVariable("set", "typo", _ => 1).Run(Context(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("typo");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALoopConditionCanBeWrittenInTheFilterExpressionLanguage()
    {
        var condition = FlowCondition.FromExpression("Attempt < 2", NullLogger.Instance);
        condition.IsSuccess.ShouldBeTrue(condition.CurrentMessage);
        var context = Context();
        context.Variables.Declare("Attempt", 0);
        var runs = 0;
        var loop = new FlowDoWhile(
            "retry", condition.Value!, 10,
            [
                Step("n", _ =>
                {
                    runs++;
                    return Done();
                }),
                new FlowSetVariable("advance", "Attempt", v =>
                {
                    v.TryGet("Attempt", out var a);
                    return (int)a! + 1;
                })
            ]);

        await loop.Run(context, TestContext.Current.CancellationToken);

        runs.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ExecutePipelineStartsTheChildUnderTheRunWithParametersMadeFromVariablesAndCapturesItsOutcome()
    {
        var starter = new FakeRunStarter();
        var context = Context(starter, Identity);
        context.Variables.Declare("region", "ERCOT");
        context.Variables.Declare("ok", false);
        var step = new FlowExecutePipeline(
            "run child", "Consolidate", [new FlowStepParameter("Region", v =>
            {
                v.TryGet("region", out var r);
                return (string)r!;
            })], awaitCompletion: true, resultVariable: "ok", failOnChildFailure: true);

        var result = await step.Run(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var start = starter.Started.ShouldHaveSingleItem();
        start.PipelineName.ShouldBe("Consolidate");
        start.ParentExecutionId.ShouldBe(Identity.ExecutionId);
        start.TenantId.ShouldBe(Identity.TenantId);
        start.RunRoles.ShouldBe(Identity.RunRoles);
        start.Parameters.ShouldHaveSingleItem().Value.ShouldBe("ERCOT");
        start.AwaitCompletion.ShouldBeTrue();
        context.Variables.TryGet("ok", out var ok).ShouldBeTrue();
        ok.ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecutePipelineFailsWhenTheChildFailsAndTheStepIsSetToFailOnIt()
    {
        var starter = new FakeRunStarter { AwaitResult = false };
        var context = Context(starter, Identity);
        context.Variables.Declare("ok", true);
        var step = new FlowExecutePipeline("run child", "Consolidate", [], true, "ok", true);

        var result = await step.Run(context, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        context.Variables.TryGet("ok", out var ok);
        ok.ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecutePipelineWithoutAStarterOrATrackedRunFailsAndStartsNothing()
    {
        var starter = new FakeRunStarter();
        var step = new FlowExecutePipeline("run child", "Consolidate", [], false, null, false);

        var noStarter = await step.Run(Context(null, Identity), TestContext.Current.CancellationToken);
        var untracked = await step.Run(Context(starter, null), TestContext.Current.CancellationToken);

        noStarter.IsFailure.ShouldBeTrue();
        untracked.IsFailure.ShouldBeTrue();
        starter.Started.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALoopRunsAStageEachIterationAndSeesTheCounterInTheStagesOwnComponents()
    {
        var context = Context();
        var tags = new List<int>();
        var loop = new FlowForLoop("per region", "n", 1, 3, 1, [
            new FlowStageStep("load", Graph(
                [Source("src", 2), new FlowDestination("dst", (_, _) => Done())],
                [Link("src", "dst")])),
            Step("note", c =>
            {
                c.Variables.TryGet("n", out var n);
                tags.Add((int)n!);
                return Done();
            })]);

        var result = await loop.Run(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        tags.ShouldBe(new[] { 1, 2, 3 });
    }
}

/// <summary>A control-flow step a test writes inline.</summary>
internal sealed class ScriptedStep : IFlowStep
{
    private readonly Func<FlowStepContext, Task<IGenericResult>> _run;

    public ScriptedStep(string name, Func<FlowStepContext, Task<IGenericResult>> run)
    {
        Name = name;
        _run = run;
    }

    public string Name { get; }

    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default) => _run(context);
}
