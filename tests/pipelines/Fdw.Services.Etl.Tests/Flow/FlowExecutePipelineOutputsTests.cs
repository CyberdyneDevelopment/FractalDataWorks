using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// FDW-6136: an Execute pipeline step that waits reads the outputs of the pipeline it started into variables. Each value keeps the type the started pipeline states for it, and a variable that is declared
/// with another type, an output the run did not record, or a value that does not read as its type fails the step with its own message and sets nothing.
/// </summary>
public sealed class FlowExecutePipelineOutputsTests
{
    private static readonly FlowRunIdentity Identity = new(
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), ["etl-runner"]);

    private static FlowStepContext Context(FakeRunStarter starter, FlowVariables variables) =>
        new(variables, new FlowRunTally(new FlowExecutor()), Options(), starter, Identity);

    // A variable the graph declares is in scope with the value it was declared with, here none: TryGet answers false only for a blank parameter or a name nobody declared, so "nothing was set" is a declared variable still holding null.
    private static void VariableHoldsNull(FlowVariables variables, string name)
    {
        variables.TryGet(name, out var value).ShouldBeTrue($"{name} is declared");
        value.ShouldBeNull();
    }

    private static FlowVariables Variables()
    {
        var variables = new FlowVariables();
        variables.Declare("Total", null, "Int32");
        variables.Declare("Label", null, "String");
        variables.Declare("Plain", null);
        return variables;
    }

    private static FlowExecutePipeline Step(params (string Output, string Variable)[] map) =>
        new("report", FlowPipelineTarget.Named("Child"), [], true, null, false, outputs: [.. Array.ConvertAll(map, pair => new KeyValuePair<string, string>(pair.Output, pair.Variable))]);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOutputIsReadIntoAVariableOfItsOwnTypeNotAsText()
    {
        var starter = new FakeRunStarter();
        starter.RecordedOutputs.Add(new PipelineOutputValue("total", "Int32", "42"));
        starter.RecordedOutputs.Add(new PipelineOutputValue("label", "String", "done"));
        var variables = Variables();

        var ran = await Step(("total", "Total"), ("label", "Label")).Run(Context(starter, variables), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        variables.TryGet("Total", out var total).ShouldBeTrue();
        total.ShouldBe(42);
        variables.TryGet("Label", out var label).ShouldBeTrue();
        label.ShouldBe("done");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOutputTheRunDidNotRecordFailsTheStepNamingIt()
    {
        var starter = new FakeRunStarter();
        var variables = Variables();

        var ran = await Step(("total", "Total")).Run(Context(starter, variables), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("'total'");
        VariableHoldsNull(variables, "Total");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Label")]
    [InlineData("Plain")]
    public async Task AVariableDeclaredWithAnotherTypeOrNoneRefusesTheOutputAndSetsNothing(string variable)
    {
        var starter = new FakeRunStarter();
        starter.RecordedOutputs.Add(new PipelineOutputValue("total", "Int32", "42"));
        var variables = Variables();

        var ran = await Step(("total", variable)).Run(Context(starter, variables), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        VariableHoldsNull(variables, variable);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AValueThatDoesNotReadAsItsTypeFailsTheStep()
    {
        var starter = new FakeRunStarter();
        starter.RecordedOutputs.Add(new PipelineOutputValue("total", "Int32", "forty-two"));
        var variables = Variables();

        var ran = await Step(("total", "Total")).Run(Context(starter, variables), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        VariableHoldsNull(variables, "Total");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOutputRecordedWithNoValueSetsTheVariableNull()
    {
        var starter = new FakeRunStarter();
        starter.RecordedOutputs.Add(new PipelineOutputValue("total", "Int32", null));
        var variables = Variables();
        variables.TrySet("Total", 7);

        var ran = await Step(("total", "Total")).Run(Context(starter, variables), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        VariableHoldsNull(variables, "Total");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChildThatFailedGivesNothingAndNothingIsRead()
    {
        var starter = new FakeRunStarter { AwaitResult = false };
        starter.RecordedOutputs.Add(new PipelineOutputValue("total", "Int32", "42"));
        var variables = Variables();

        var ran = await Step(("total", "Total")).Run(Context(starter, variables), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        VariableHoldsNull(variables, "Total");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AStepThatDoesNotWaitCannotReadOutputs()
    {
        Should.Throw<ArgumentException>(() => new FlowExecutePipeline(
            "report", FlowPipelineTarget.Named("Child"), [], false, null, false, outputs: [new KeyValuePair<string, string>("total", "Total")]));
    }
}
