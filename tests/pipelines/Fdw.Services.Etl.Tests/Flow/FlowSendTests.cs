using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// FDW-6138: the Send component. Its subject, message and payload fields are made from the variables when it runs, a payload type states the payload, a run that counts no rows can send, a send that
/// fails fails the step, and a variable with no value fails it with its own message and sends nothing. The save refuses a payload type that is none, a field the type does not declare and one it requires.
/// </summary>
public sealed class FlowSendTests
{
    private static readonly FlowNotifyRun Run = new(Guid.NewGuid(), "Chat", Guid.NewGuid(), DateTimeOffset.UtcNow);

    private static FlowStepContext Context(string? name)
    {
        var variables = new FlowVariables();
        variables.Declare("Name", name);
        variables.Declare("Answer", "It is sunny");
        return new(variables, new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null);
    }

    private static FlowSend Step(FakeRuleNotifier notifier, string? payloadType = "Reply", string rule = "replies") =>
        new(
            "answer",
            notifier,
            rule,
            "Hello {{Name}}",
            "{{Answer}}",
            payloadType,
            [new FlowStepParameter("text", variables => VariableText.Render("{{Answer}} for {{Name}}", variables))],
            Run);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheWordsAndThePayloadFieldsAreMadeFromTheVariablesAndSentThroughTheRule()
    {
        var notifier = new FakeRuleNotifier([]);

        var ran = await Step(notifier).Run(Context("Mike"), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        var sent = notifier.Sent.ShouldHaveSingleItem();
        sent.RuleName.ShouldBe("replies");
        sent.Subject.ShouldBe("Hello Mike");
        sent.Message.ShouldBe("It is sunny");
        sent.PipelineId.ShouldBe(Run.PipelineId);
        sent.CorrelationId.ShouldBe(Run.ExecutionId.ToString());
        var payload = sent.Payload.ShouldNotBeNull();
        payload.TypeName.ShouldBe("Reply");
        payload.Fields["text"].ShouldBe("It is sunny for Mike");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AStepWithNoPayloadTypeSendsTheWordsOnlyAndNeedsNoRowCounts()
    {
        var notifier = new FakeRuleNotifier([]);

        var ran = await Step(notifier, payloadType: null).Run(Context("Mike"), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        notifier.Sent.ShouldHaveSingleItem().Payload.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASendThatFailsFailsTheStepNamingTheRule()
    {
        var notifier = new FakeRuleNotifier([]);
        notifier.FailingRules.Add("replies");

        var ran = await Step(notifier).Run(Context("Mike"), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("'replies'");
        ran.CurrentMessage.ShouldContain("refused");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AVariableWithNoValueFailsTheStepAndSendsNothing()
    {
        var notifier = new FakeRuleNotifier([]);
        var context = new FlowStepContext(new FlowVariables(), new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null);

        var ran = await Step(notifier).Run(context, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("could not make its words");
        notifier.Sent.ShouldBeEmpty();
    }

    private static PipelineComponentConfiguration Send(string? payloadType, params string[] fields)
    {
        var component = new PipelineComponentConfiguration
        {
            Name = "answer", ComponentType = "Send", NotificationRuleName = "replies", NotificationMessage = "{{Answer}}",
        };
        component.NotificationSubject = "Hello";
        component.NotificationPayloadType = payloadType;

        foreach (var field in fields)
        {
            component.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = field, ValueText = "{{Answer}}" });
        }

        return component;
    }

    private static string[] Reasons(PipelineComponentConfiguration component)
    {
        var graph = new GraphPipelineConfiguration { Name = "Sample" };
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Answer", DataType = "String", ValueText = "x" });
        graph.Components.Add(component);
        return [.. GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Select(problem => problem.Reason)];
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASoundSendHasNoProblemWithOrWithoutAPayload()
    {
        Reasons(Send(null)).ShouldBeEmpty();
        Reasons(Send("Reply", "text")).ShouldBeEmpty();
        Reasons(Send("Card", "title", "body", "actions")).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void APayloadTypeThatIsNoneAFieldItDoesNotDeclareAFieldItRequiresAndFieldsWithNoTypeAreEachRefused()
    {
        Reasons(Send("Poem", "text")).ShouldContain(reason => reason.Contains("'Poem', which is not a payload type"));
        Reasons(Send("Reply", "text", "colour")).ShouldHaveSingleItem().ShouldContain("'colour', which the payload type 'Reply' does not declare");
        Reasons(Send("Card", "title")).ShouldHaveSingleItem().ShouldContain("'body', which the payload type 'Card' requires");
        Reasons(Send(null, "text")).ShouldHaveSingleItem().ShouldContain("states payload fields and no payload type");
    }
}
