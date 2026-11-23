using Fdw.Services.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Execution.Answering;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// FDW-6142: the Ask component. It judges a call under a policy: a call that goes on fires <c>go</c> and asks nobody, a call the policy refuses fails the step, and a call that needs the person keeps a request
/// for the person the run acts as, tells them through the rule and fires only <c>asked</c>. What is kept is made from the variables when the step runs; a run that acts as no person, a continuation that is none, a
/// parameter the continuation does not declare and a notification that fails each stop it with their own message. The builder and the save refuse what cannot work.
/// </summary>
public sealed class FlowAskTests
{
    private static readonly Guid Person = Guid.Parse("77777777-0000-0000-0000-000000000042");
    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000042");
    private static readonly Guid Run = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Root = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Reply = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid RequestId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    private static readonly FlowNotifyRun NotifyRun = new(Guid.NewGuid(), "Chat", Run, DateTimeOffset.UtcNow);

    private readonly Mock<IAnswerRequests> _requests = new();
    private readonly Mock<IAnswerPolicies> _policies = new();
    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private readonly FakeRuleNotifier _notifier = new([]);
    private readonly List<AnswerRequestAsk> _asks = [];
    private DateTimeOffset? _announced;

    public FlowAskTests()
    {
        _pipelines.Setup(p => p.Get("Reply",ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(Pipeline("Reply", Reply, "order", "reply")));
        _policies.Setup(p => p.Judge("tools", It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string policy, string call, Guid? _, CancellationToken __) => GenericResult<AnswerPolicyJudgment>.Success(new AnswerPolicyJudgment(policy, call, AnswerPolicyOutcomes.ByName("NeedsPerson"), 900)));
        _requests.Setup(r => r.Ask(It.IsAny<AnswerRequestAsk>(), It.IsAny<CancellationToken>()))
            .Callback((AnswerRequestAsk ask, CancellationToken _) => _asks.Add(ask))
            .ReturnsAsync((AnswerRequestAsk ask, CancellationToken _) => GenericResult<AnswerRequestKept>.Success(new AnswerRequestKept(Request(ask), true)));
        _requests.Setup(r => r.Announced(Tenant, RequestId, It.IsAny<CancellationToken>())).Callback(() => _announced = DateTimeOffset.UtcNow).ReturnsAsync(GenericResult.Success());
    }

    private static IPipelineImplementationConfiguration Pipeline(string name, Guid id, params string[] parameters)
    {
        var pipeline = new Mock<IPipelineImplementationConfiguration>();
        pipeline.SetupGet(p => p.Name).Returns(name);
        pipeline.SetupGet(p => p.Id).Returns(id);
        pipeline.As<IDeclaresRunParameters>().SetupGet(p => p.DeclaredRunParameters)
            .Returns([.. parameters.Select(parameter => Mock.Of<IPipelineRunParameter>(p => p.Name == parameter && p.IsRequired == true && p.ParameterType == "String"))]);
        return pipeline.Object;
    }

    private static AnswerRequest Request(AnswerRequestAsk ask, DateTimeOffset? announced = null) => new()
    {
        Id = RequestId,
        TenantId = ask.TenantId,
        PersonId = ask.PersonId,
        ExecutionId = ask.ExecutionId,
        Kind = AnswerRequestKinds.ByName(ask.KindName),
        PolicyName = ask.PolicyName,
        CallName = ask.CallName,
        State = AnswerRequestStates.ByName("Open"),
        AskedAt = DateTimeOffset.UtcNow,
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ask.ExpiresAfterSeconds),
        AnnouncedAt = announced,
        Shown = ask.Shown,
    };

    private FlowAskStep Step(string kind = "Question", string[]? options = null, string? grantVariable = null, string continuation = "Reply", string answerParameter = "reply", string pipeline = "Chat") =>
        new(
            "ask",
            new AskStepSettings(
                "tools", "{{Tool}}", kind, continuation, answerParameter, "chat-replies", "Run {{Tool}}?", "For {{Name}}", [.. options ?? []], grantVariable,
                [new FlowStepParameter("order", variables => VariableText.Render("{{Order}}", variables))]),
            new FlowAskServices(() => _requests.Object, () => _policies.Object, () => _pipelines.Object),
            _notifier,
            NotifyRun with { PipelineName = pipeline },
            NullLogger.Instance);

    private static FlowStepContext Context(bool withPerson = true, string? grant = null)
    {
        var variables = new FlowVariables();
        variables.Declare("Tool", "send_email");
        variables.Declare("Name", "Mike");
        variables.Declare("Order", "42");
        if (grant is not null)
        {
            variables.Declare("Grant", grant);
        }

        return new FlowStepContext(variables, new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, new FlowRunIdentity(Run, Tenant, ["Operator"]) { PersonId = withPerson ? Person : null });
    }

    private static bool Fired(FlowStepContext context, string output) => context.Variables.TryGet(FlowFired.VariableOf("ask", output), out var value) && value is true;

    private void Judges(string outcome) =>
        _policies.Setup(p => p.Judge("tools", It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string policy, string call, Guid? _, CancellationToken __) => GenericResult<AnswerPolicyJudgment>.Success(new AnswerPolicyJudgment(policy, call, AnswerPolicyOutcomes.ByName(outcome), 900)));

    [Theory]
    [InlineData("Automatic")]
    [InlineData("Granted")]
    [Trait("Priority", "P0")]
    public async Task ACallThatMayGoOnFiresGoAndAsksNobody(string outcome)
    {
        Judges(outcome);
        var context = Context();

        var ran = await Step().Run(context, TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Fired(context, "go").ShouldBeTrue();
        Fired(context, "asked").ShouldBeFalse();
        _requests.Verify(r => r.Ask(It.IsAny<AnswerRequestAsk>(), It.IsAny<CancellationToken>()), Times.Never);
        _notifier.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallThePolicyRefusesFailsTheStepFiresNothingAndAsksNobody()
    {
        Judges("WouldBeRefused");
        var context = Context();

        var ran = await Step().Run(context, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("refuses call 'send_email'");
        Fired(context, "go").ShouldBeFalse();
        Fired(context, "asked").ShouldBeFalse();
        _asks.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallThatNeedsThePersonKeepsARequestForThePersonTheRunActsAsTellsThemAndFiresOnlyAsked()
    {
        var context = Context();

        var ran = await Step().Run(context, TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Fired(context, "asked").ShouldBeTrue();
        Fired(context, "go").ShouldBeFalse();
        var ask = _asks.ShouldHaveSingleItem();
        ask.PersonId.ShouldBe(Person);
        ask.TenantId.ShouldBe(Tenant);
        ask.ExecutionId.ShouldBe(Run);
        ask.ClaimRootId.ShouldBe(Run);
        ask.ComponentName.ShouldBe("ask");
        ask.Iteration.ShouldBe(0);
        ask.PolicyName.ShouldBe("tools");
        ask.CallName.ShouldBe("send_email");
        ask.ExpiresAfterSeconds.ShouldBe(900);
        ask.ContinuationPipelineId.ShouldBe(Reply);
        ask.AnswerParameterName.ShouldBe("reply");
        ask.RunRoles.ShouldBe(["Operator"]);
        ask.Shown.Select(pair => (pair.Key, pair.Value)).ShouldBe([("title", "Run send_email?"), ("body", "For Mike")]);
        ask.Arguments.Select(pair => (pair.Key, pair.Value)).ShouldBe([("order", "42")]);
        var sent = _notifier.Sent.ShouldHaveSingleItem();
        sent.RuleName.ShouldBe("chat-replies");
        sent.Payload.ShouldNotBeNull().TypeName.ShouldBe("Card");
        sent.Payload.Fields["title"].ShouldBe("Run send_email?");
        sent.Metadata["AnswerRequestId"].ShouldBe(RequestId);
        sent.Metadata["PersonId"].ShouldBe(Person);
        _announced.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChoiceShowsItsOptionsAndTheCardCarriesThemAsActions()
    {
        var ran = await Step("Choice", ["yes", "no"]).Run(Context(), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        _asks.ShouldHaveSingleItem().Shown.Where(pair => pair.Key == "option").Select(pair => pair.Value).ShouldBe(["yes", "no"]);
        _notifier.Sent.ShouldHaveSingleItem().Payload.ShouldNotBeNull().Fields["actions"].ShouldBe("yes\nno");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARequestThePersonWasToldAboutIsNotAnnouncedAgainWhenTheStepReachesItAgain()
    {
        _requests.Setup(r => r.Ask(It.IsAny<AnswerRequestAsk>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnswerRequestAsk ask, CancellationToken _) => GenericResult<AnswerRequestKept>.Success(new AnswerRequestKept(Request(ask, DateTimeOffset.UtcNow), false)));
        var context = Context();

        var ran = await Step().Run(context, TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Fired(context, "asked").ShouldBeTrue();
        _notifier.Sent.ShouldBeEmpty();
        _announced.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ANotificationThatFailsFailsTheStepAndTheRequestStaysKeptButIsNotRecordedAsAnnounced()
    {
        _notifier.FailingRules.Add("chat-replies");
        var context = Context();

        var ran = await Step().Run(context, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("was not told");
        Fired(context, "asked").ShouldBeFalse();
        _asks.Count.ShouldBe(1);
        _announced.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARunThatActsAsNoPersonCannotAskAndNobodyIsAssumed()
    {
        var ran = await Step().Run(Context(withPerson: false), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("acts as no person");
        _asks.ShouldBeEmpty();
        _policies.Verify(p => p.Judge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AContinuationThatIsNoPipelineIsThePipelineAskingOrDoesNotDeclareAParameterIsRefusedAndNothingIsKept()
    {
        _pipelines.Setup(p => p.Get("Missing",ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Failure(new GenericMessage("no such pipeline")));
        _pipelines.Setup(p => p.Get("Chat",ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(Pipeline("Chat", Guid.NewGuid(), "order", "reply")));

        (await Step(continuation: "Missing").Run(Context(), TestContext.Current.CancellationToken)).CurrentMessage.ShouldNotBeNull().ShouldContain("could not be found");
        (await Step(continuation: "Chat").Run(Context(), TestContext.Current.CancellationToken)).CurrentMessage.ShouldNotBeNull().ShouldContain("its own continuation");
        (await Step(answerParameter: "answer").Run(Context(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        _asks.ShouldBeEmpty();
        _notifier.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheGrantAVariableHoldsIsGivenToThePolicyAndAVariableThatHoldsNoGrantIsRefused()
    {
        var grant = Guid.Parse("22222222-0000-0000-0000-000000000042");

        (await Step(grantVariable: "Grant").Run(Context(grant: grant.ToString("D")), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        _policies.Verify(p => p.Judge("tools", "send_email", grant, It.IsAny<CancellationToken>()), Times.Once);

        var refused = await Step(grantVariable: "Grant").Run(Context(grant: "not a grant"), TestContext.Current.CancellationToken);
        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("not a grant id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AStepInALoopAsksOnceForEachRoundUnderItsOwnIteration()
    {
        var step = Step();

        await step.Run(Context(), TestContext.Current.CancellationToken);
        await step.Run(Context(), TestContext.Current.CancellationToken);

        _asks.Select(ask => ask.Iteration).ShouldBe([0, 1]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AVariableWithNoValueFailsTheStepAndKeepsNothing()
    {
        var context = new FlowStepContext(new FlowVariables(), new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, new FlowRunIdentity(Run, Tenant, ["Operator"]) { PersonId = Person });

        var ran = await Step().Run(context, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("could not make the words");
        _asks.ShouldBeEmpty();
    }

    private static PipelineComponentConfiguration Component(string kind = "Question", string? options = null, params string[] arguments)
    {
        var component = new PipelineComponentConfiguration { Name = "ask", ComponentType = "Ask", NotificationRuleName = "chat-replies" };
        foreach (var (name, value) in new Dictionary<string, string?>
        {
            ["policy"] = "tools", ["call"] = "send_email", ["kind"] = kind, ["continuation"] = "Reply", ["answerParameter"] = "reply", ["title"] = "Send?", ["body"] = "Really?", ["options"] = options,
        })
        {
            if (value is not null)
            {
                ComponentSettingBindings.Find(name).ShouldNotBeNull(name).Set(component, value);
            }
        }

        foreach (var argument in arguments)
        {
            component.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = argument, ValueText = "{{Order}}" });
        }

        return component;
    }

    private static string[] Reasons(PipelineComponentConfiguration component)
    {
        var graph = new GraphPipelineConfiguration { Name = "Sample" };
        graph.Components.Add(component);
        return [.. GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Select(problem => problem.Reason)];
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheSaveRefusesAKindThatIsNoneAChoiceWithNoOptionsAQuestionWithSomeAndAnArgumentNamedLikeTheAnswer()
    {
        Reasons(Component("Poll")).ShouldContain(reason => reason.Contains("'Poll', which is none of the kinds"));
        Reasons(Component("Choice")).ShouldContain(reason => reason.Contains("asks a choice and states no options"));
        Reasons(Component("Question", "yes\nno")).ShouldContain(reason => reason.Contains("asks a question and states options"));
        Reasons(Component(arguments: "reply")).ShouldContain(reason => reason.Contains("which is the parameter the answer goes to"));
        Reasons(Component("Choice", "yes\nno", arguments: "order")).ShouldNotContain(reason => reason.Contains("options") || reason.Contains("kinds") || reason.Contains("answer goes to"));
    }

    [Theory]
    [InlineData("AnswerRequest:1f0e")]
    [InlineData("answerrequest:x")]
    [Trait("Priority", "P0")]
    public void AComponentNamedLikeTheClaimOfAnAnsweredStepIsRefusedAtSave(string name)
    {
        var component = Component();
        component.Name = name;

        Reasons(component).ShouldContain(reason => reason.Contains("how the claim of an answered step is called"));
        Reasons(Component()).ShouldNotContain(reason => reason.Contains("how the claim of an answered step is called"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheComponentIsADecisionWithTwoOutputsAndTheContinuationIsAPipelineItCalls()
    {
        var kind = ComponentTypes.ByName("Ask");

        (kind is IDecisionComponentType).ShouldBeTrue();
        DecisionTests.OutputsOf(Component()).ShouldBe(["go", "asked"]);
        kind.IsAction.ShouldBeTrue();
        kind.Settings.Where(setting => setting.IsRequired).Select(setting => setting.Name).ShouldBe(["rule", "policy", "call", "kind", "continuation", "answerParameter", "title", "body"]);
        AskComponentType.OptionsOf("yes\n  no \n\n").ShouldBe(["yes", "no"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheBuilderNeedsNoContributionLikeFdwsOtherKindsAndStatesNoOutsideEffectBecauseTheRequestIsTheStepsOwnClaim()
    {
        FlowStepBuilders.ByName("Ask").Needs.ShouldBeEmpty();
        FlowStepBuilders.ByName("Ask").ShouldBeAssignableTo<FlowStepBuilderBase>().HasOutsideEffect.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task InATestRunTheStepJudgesNothingFiresNeitherOutputAndKeepsNothing()
    {
        var context = Context();

        var ran = await new FlowAskTestStep("ask", NullLogger.Instance).Run(context, TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Fired(context, "go").ShouldBeFalse();
        Fired(context, "asked").ShouldBeFalse();
        _asks.ShouldBeEmpty();
    }
}
