using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Execution.Answering;
using Fdw.Services.Etl.Tests.Flow;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Answering;

/// <summary>
/// FDW-6142: the answer requests of runs. A request is kept with what the person is shown, the continuation's arguments and its roles, and survives a restart of the host; only the person it was kept for can
/// answer it; the answer states its text and nothing else; a second answer starts nothing and two at once start the continuation once; a request ends when its policy said and starts nothing then; a start that
/// failed, or that never recorded how it ended, is not made again. The operational store is stood in for by a table that behaves as the engine does (a unique key, one update judged by the rows it changed).
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class AnswerRequestsTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000042");
    private static readonly Guid Person = Guid.Parse("77777777-0000-0000-0000-000000000042");
    private static readonly Guid Another = Guid.Parse("77777777-0000-0000-0000-000000000043");
    private static readonly Guid AskingRun = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Root = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Continuation = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid ContinuationRun = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly string? _before = PipelineTypes.OperationalConnection;
    private readonly InMemoryOperationalStore _store = new();
    private readonly InMemoryStepClaims _claims = new();
    private readonly Mock<IPipelineRuns> _runs = new();
    private readonly List<PipelineStart> _starts = [];
    private readonly Clock _clock = new() { Now = Now };

    public AnswerRequestsTests()
    {
        PipelineTypes.OperationalConnection = "Ops";
        _runs.Setup(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<CancellationToken>()))
            .Callback((PipelineStart start, CancellationToken _) => _starts.Add(start))
            .ReturnsAsync(GenericResult<Guid>.Success(ContinuationRun));
    }

    public void Dispose() => PipelineTypes.OperationalConnection = _before;

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private AnswerRequests Sut(Guid? person = null, bool isPerson = true, Guid? tenant = null)
    {
        var gateways = new Mock<IDataGatewayProvider>();
        gateways.Setup(provider => provider.ByName("Main")).Returns(_store.Gateway.Object);
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(isPerson);
        context.SetupGet(c => c.IsSystemContext).Returns(false);
        context.SetupGet(c => c.UserId).Returns((person ?? Person).ToString("D"));
        context.SetupGet(c => c.ActiveTenantId).Returns(tenant ?? Tenant);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return new AnswerRequests(gateways.Object, accessor.Object, _runs.Object, _claims, _clock, NullLogger<AnswerRequests>.Instance);
    }

    private static AnswerRequestAsk AskOf(string kind = "Question", int iteration = 0, int seconds = 3600, string answerParameter = "reply", params string[] options) => new()
    {
        TenantId = Tenant,
        PersonId = Person,
        ExecutionId = AskingRun,
        ClaimRootId = Root,
        ComponentName = "Ask",
        Iteration = iteration,
        KindName = kind,
        PolicyName = "tools",
        CallName = "send_email",
        ExpiresAfterSeconds = seconds,
        ContinuationPipelineId = Continuation,
        AnswerParameterName = answerParameter,
        Shown =
        [
            new KeyValuePair<string, string>(AnswerRequestShown.Title, "Send the email?"),
            new KeyValuePair<string, string>(AnswerRequestShown.Body, "To Dana, about the invoice"),
            .. options.Select(option => new KeyValuePair<string, string>(AnswerRequestShown.Option, option)),
        ],
        Arguments = [new KeyValuePair<string, string>("order", "42")],
        RunRoles = ["Operator"],
    };

    private async Task<AnswerRequest> Kept(AnswerRequestAsk? ask = null)
    {
        var kept = await Sut().Ask(ask ?? AskOf(), TestContext.Current.CancellationToken);
        kept.IsSuccess.ShouldBeTrue(kept.CurrentMessage);
        return kept.Value.ShouldNotBeNull().Request;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AskingKeepsTheRequestWithWhatThePersonIsShownTheArgumentsAndTheRolesAndOpensIt()
    {
        var kept = await Sut().Ask(AskOf(), TestContext.Current.CancellationToken);

        kept.IsSuccess.ShouldBeTrue(kept.CurrentMessage);
        var value = kept.Value.ShouldNotBeNull();
        value.IsNew.ShouldBeTrue();
        value.Request.State.Name.ShouldBe("Open");
        value.Request.PersonId.ShouldBe(Person);
        value.Request.ExpiresAt.ShouldBe(Now.AddSeconds(3600));
        value.Request.Shown.Select(pair => pair.Key).ShouldBe([AnswerRequestShown.Title, AnswerRequestShown.Body]);
        _store.Requests.ShouldHaveSingleItem().StateName.ShouldBe("Open");
        _store.Arguments.ShouldHaveSingleItem().ValueText.ShouldBe("42");
        _store.Roles.ShouldHaveSingleItem().RoleName.ShouldBe("Operator");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStepThatAsksAgainGetsTheRequestItKeptAndNoSecondOne()
    {
        var first = await Kept();

        var again = await Sut().Ask(AskOf(), TestContext.Current.CancellationToken);

        again.IsSuccess.ShouldBeTrue(again.CurrentMessage);
        again.Value.ShouldNotBeNull().IsNew.ShouldBeFalse();
        again.Value.Request.Id.ShouldBe(first.Id);
        _store.Requests.Count.ShouldBe(1);
        _store.Shown.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARequestKeptBeforeTheHostRestartedIsListedToItsPersonByAHostThatKnowsNothingOfIt()
    {
        var kept = await Kept();

        var restarted = await Sut().List(TestContext.Current.CancellationToken);

        restarted.IsSuccess.ShouldBeTrue(restarted.CurrentMessage);
        restarted.Value.ShouldNotBeNull().ShouldHaveSingleItem().Id.ShouldBe(kept.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AHostThatWentDownBeforeThePartsWereWrittenLeavesARequestNobodyCanReadOrAnswerAndTheNextAskFinishesIt()
    {
        _store.Requests.Add(new EtlPipelineAnswerRequestRecord
        {
            TenantId = Tenant, PersonId = Person, ExecutionId = AskingRun, ClaimRootId = Root, ComponentName = "Ask", Iteration = 0, KindName = "Question", PolicyName = "tools", CallName = "send_email",
            ContinuationPipelineId = Continuation, AnswerParameterName = "reply", StateName = "Keeping", AskedAt = Now, ExpiresAt = Now.AddHours(1),
        });

        (await Sut().List(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().ShouldBeEmpty();
        (await Sut().Answer(_store.Requests[0].Id, "yes", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        _starts.ShouldBeEmpty();

        var finished = await Sut().Ask(AskOf(), TestContext.Current.CancellationToken);

        finished.IsSuccess.ShouldBeTrue(finished.CurrentMessage);
        finished.Value.ShouldNotBeNull().Request.State.Name.ShouldBe("Open");
        _store.Requests.Count.ShouldBe(1);
        _store.Shown.Count.ShouldBe(2);
        _store.Roles.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARequestIsAnnouncedWhenTheFactIsRecordedAndNotBefore()
    {
        var kept = await Kept();
        kept.AnnouncedAt.ShouldBeNull();

        (await Sut().Announced(Tenant, kept.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _store.Requests.ShouldHaveSingleItem().AnnouncedAt.ShouldBe(Now);
        (await Sut().Announced(Tenant, Guid.NewGuid(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task OnlyThePersonTheRequestWasKeptForCanAnswerItAndNothingIsStarted()
    {
        var kept = await Kept();

        var refused = await Sut(person: Another).Answer(kept.Id, "yes", TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("another person");
        _starts.ShouldBeEmpty();
        _store.Requests.ShouldHaveSingleItem().StateName.ShouldBe("Open");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerThatIsNoPersonCanNeitherListNorAnswerAndAnotherTenantsRequestIsNotFound()
    {
        var kept = await Kept();

        (await Sut(isPerson: false).List(TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut(isPerson: false).Answer(kept.Id, "yes", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        var elsewhere = await Sut(tenant: Guid.NewGuid()).Answer(kept.Id, "yes", TestContext.Current.CancellationToken);

        elsewhere.IsFailure.ShouldBeTrue();
        elsewhere.CurrentMessage.ShouldNotBeNull().ShouldContain("not a request of the caller's tenant");
        _starts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AnAnswerCallStatesTheRequestAndTheTextAndNothingElse()
    {
        var method = typeof(IAnswerRequests).GetMethod(nameof(IAnswerRequests.Answer)) ?? throw new InvalidOperationException("IAnswerRequests has no Answer.");

        method.GetParameters().Select(parameter => parameter.Name).ShouldBe(["requestId", "answer", "cancellationToken"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheAnswerStartsTheContinuationFromWhatWasSavedWithTheRequestAndTheAnswerAsThePerson()
    {
        var kept = await Kept();

        var answered = await Sut().Answer(kept.Id, "yes please", TestContext.Current.CancellationToken);

        answered.IsSuccess.ShouldBeTrue(answered.CurrentMessage);
        var value = answered.Value.ShouldNotBeNull();
        value.ContinuationExecutionId.ShouldBe(ContinuationRun);
        value.Request.State.Name.ShouldBe("Started");
        var start = _starts.ShouldHaveSingleItem();
        start.PipelineId.ShouldBe(Continuation);
        start.TenantId.ShouldBe(Tenant);
        start.RunRoles.ShouldBe(["Operator"]);
        start.Parameters.Select(pair => (pair.Key, pair.Value)).ShouldBe([("order", "42"), ("reply", "yes please")]);
        start.ParentExecutionId.ShouldBe(AskingRun);
        start.TriggerSource.ShouldBe($"Answer:{kept.Id:D}");
        start.AwaitCompletion.ShouldBeFalse();
        start.PersonId.ShouldBeNull();
        _claims.Made.ShouldHaveSingleItem().Root.ShouldBe(Root);
        _claims.OutcomeOf(Root, $"AnswerRequest:{kept.Id:D}", 0).ShouldBe("Succeeded");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASecondAnswerStartsNothing()
    {
        var kept = await Kept();
        (await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        var second = await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken);

        second.IsFailure.ShouldBeTrue();
        second.CurrentMessage.ShouldNotBeNull().ShouldContain("takes no answer");
        _starts.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TwoAnswersAtOnceStartTheContinuationOnce()
    {
        var kept = await Kept();

        var results = await Task.WhenAll(Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken), Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken));

        results.Count(result => result.IsSuccess).ShouldBe(1);
        _starts.Count.ShouldBe(1);
        _claims.Made.Count.ShouldBe(1);
        _store.Requests.ShouldHaveSingleItem().StateName.ShouldBe("Started");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnAnswerAfterTheEndTheRequestStatedIsRefusedStartsNothingAndTheRequestExpires()
    {
        var kept = await Kept(AskOf(seconds: 60));
        _clock.Now = Now.AddSeconds(61);

        var late = await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken);

        late.IsFailure.ShouldBeTrue();
        late.CurrentMessage.ShouldNotBeNull().ShouldContain("ended at");
        _starts.ShouldBeEmpty();
        _store.Requests.ShouldHaveSingleItem().StateName.ShouldBe("Expired");
        (await Sut().List(TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARequestThatHasNotEndedIsStillOpenAtTheLastSecondItStated()
    {
        var kept = await Kept(AskOf(seconds: 60));
        _clock.Now = Now.AddSeconds(59);

        (await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        _starts.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AChoiceTakesOneOfItsOptionsAndNothingElse()
    {
        var kept = await Kept(AskOf("Choice", options: ["yes", "no"]));

        var refused = await Sut().Answer(kept.Id, "maybe", TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        _starts.ShouldBeEmpty();
        _store.Requests.ShouldHaveSingleItem().StateName.ShouldBe("Open");
        (await Sut().Answer(kept.Id, "no", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        _starts.ShouldHaveSingleItem().Parameters.ShouldContain(new KeyValuePair<string, string>("reply", "no"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ABlankAnswerIsRefusedAndTheRequestStaysOpen()
    {
        var kept = await Kept();

        (await Sut().Answer(kept.Id, "  ", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        _store.Requests.ShouldHaveSingleItem().StateName.ShouldBe("Open");
        _starts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartThatFailedIsClaimedAsFailedAndNotMadeAgain()
    {
        _runs.Setup(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("the pipeline is not startable")));
        var kept = await Kept();

        (await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        var again = await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken);

        again.IsFailure.ShouldBeTrue();
        again.CurrentMessage.ShouldNotBeNull().ShouldContain("start failed there");
        _runs.Verify(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<CancellationToken>()), Times.Once);
        _claims.OutcomeOf(Root, $"AnswerRequest:{kept.Id:D}", 0).ShouldBe("Failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnAnswerTakenBeforeTheHostWentDownIsFinishedOnceByTheSameAnswerAndNeverByAnotherText()
    {
        var kept = await Kept();
        var row = _store.Requests.ShouldHaveSingleItem();
        row.StateName = "Answered";
        row.AnsweredAt = Now;
        row.AnswerText = "yes";

        var other = await Sut().Answer(kept.Id, "no", TestContext.Current.CancellationToken);
        other.IsFailure.ShouldBeTrue();
        _starts.ShouldBeEmpty();

        var finished = await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken);

        finished.IsSuccess.ShouldBeTrue(finished.CurrentMessage);
        _starts.ShouldHaveSingleItem().Parameters.ShouldContain(new KeyValuePair<string, string>("reply", "yes"));
        _store.Requests.ShouldHaveSingleItem().StateName.ShouldBe("Started");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartThatNeverRecordedHowItEndedIsNotMadeAgainBecauseNobodyCanSayItDidNotBegin()
    {
        var kept = await Kept();
        var row = _store.Requests.ShouldHaveSingleItem();
        row.StateName = "Answered";
        row.AnsweredAt = Now;
        row.AnswerText = "yes";
        _claims.Seed(Tenant, Root, $"AnswerRequest:{kept.Id:D}", 0, AskingRun, null);

        var refused = await Sut().Answer(kept.Id, "yes", TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("cannot be known");
        _starts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Choice", 3600, "reply", new string[0])]
    [InlineData("Question", 3600, "reply", new[] { "yes" })]
    [InlineData("Question", 0, "reply", new string[0])]
    [InlineData("Question", 3600, "order", new string[0])]
    [InlineData("Poll", 3600, "reply", new string[0])]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnAskThatCannotBeKeptIsRefusedAndWritesNothing(string kind, int seconds, string answerParameter, string[] options)
    {
        var refused = await Sut().Ask(AskOf(kind, seconds: seconds, answerParameter: answerParameter, options: options), TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        _store.Requests.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStoreThatCannotWriteTheRequestMakesTheAskFailLoud()
    {
        _store.FailsWritesTo = "PipelineAnswerRequest";

        var refused = await Sut().Ask(AskOf(), TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        _store.Requests.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANextHostWithNoOperationalStoreNamedRefusesEveryCallAndAssumesNone()
    {
        PipelineTypes.OperationalConnection = null;

        (await Sut().Ask(AskOf(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut().List(TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut().Answer(Guid.NewGuid(), "yes", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }
}
