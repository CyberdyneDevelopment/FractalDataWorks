using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-5347: a list that cannot be written as quoted text (a null item, or an item that holds a double quote) fails the Execute pipeline step and
/// the Set variable calculation with the coded message FLOW-21010, and not with an exception. Execute SQL has its own test beside the list rendering.
/// </summary>
public sealed class ListParameterStepTests
{
    private static readonly FlowRunIdentity Identity = new(
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), ["etl-runner"]);

    [Theory]
    [InlineData("A\"B")]
    [InlineData(null)]
    public async Task AnExecutePipelineStepWhoseParameterNamesAListItThatCannotBeQuotedFailsWithTheCodedMessageAndStartsNothing(string? item)
    {
        var starter = new FakeRunStarter();
        var context = new FlowStepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor()), Options(), starter, Identity);
        context.Variables.Declare("Names", new List<string?> { "A", item });
        var step = new FlowExecutePipeline(
            "run child", "Consolidate", [new FlowStepParameter("Region", variables => VariableText.Render("in {{Names}}", variables))],
            awaitCompletion: false, resultVariable: null, failOnChildFailure: false);

        var ran = await step.Run(context, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.Messages.ShouldContain(message => message.Code == "FLOW-21010");
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("run child");
        ran.CurrentMessage.ShouldContain("Names");
        starter.Started.ShouldBeEmpty();
    }

    private static PipelineComponentConfiguration Calculate(string input, string parameter)
    {
        var step = new PipelineComponentConfiguration
        {
            Name = "calc", ComponentType = "SetVariable", VariableName = "Clean", ConstantText = input, TransformationName = "Concatenate",
        };
        step.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "prefix", ValueText = parameter });
        step.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "suffix", ValueText = "." });
        return step;
    }

    private static async Task<(Fdw.Results.IGenericResult Result, FlowVariables Variables)> Run(PipelineComponentConfiguration step, string? item)
    {
        var variables = new FlowVariables();
        variables.Declare("Clean", null);
        variables.Declare("Stamp", "x");
        variables.Declare("Names", new List<string?> { "A", item });
        var context = ContextWith(new GraphPipelineConfiguration { Name = "Ingest" }, new Dictionary<string, object?>());
        var built = new SetVariableFlowStepBuilder().Create(step, [], context);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);

        var ran = await built.Value!.Run(StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);
        return (ran, variables);
    }

    [Theory]
    [InlineData("A\"B")]
    [InlineData(null)]
    public async Task ACalculationWhoseInputNamesAListItThatCannotBeQuotedFailsWithTheCodedMessageAndLeavesTheVariableAlone(string? item)
    {
        var (ran, variables) = await Run(Calculate("in {{Names}}", "x"), item);

        ran.IsFailure.ShouldBeTrue();
        ran.Messages.ShouldContain(message => message.Code == "FLOW-21010");
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Names");
        variables.TryGet("Clean", out var value).ShouldBeTrue();
        value.ShouldBeNull();
    }

    [Theory]
    [InlineData("A\"B")]
    [InlineData(null)]
    public async Task ACalculationWhoseParameterNamesAListItThatCannotBeQuotedFailsWithTheCodedMessageAndLeavesTheVariableAlone(string? item)
    {
        var (ran, variables) = await Run(Calculate("{{Stamp}}", "in {{Names}}"), item);

        ran.IsFailure.ShouldBeTrue();
        ran.Messages.ShouldContain(message => message.Code == "FLOW-21010");
        variables.TryGet("Clean", out var value).ShouldBeTrue();
        value.ShouldBeNull();
    }
}
