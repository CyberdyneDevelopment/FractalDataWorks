using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Acceptance criterion 2: a reasoning effort per call, and which efforts a model carries is data on its row.</summary>
public sealed class ReasoningEffortTests
{
    [Fact]
    public async Task EachCallStatesItsOwnEffortAndTheProviderGetsIt()
    {
        var configuration = StubLanguageModelConfiguration.Carrying(["Chat"], ["Low", "High"]);
        var model = StubLanguageModel.Replying(configuration, TestRequests.Plain);

        await model.Reply(TestRequests.Text("Low"), TestContext.Current.CancellationToken);
        model.LastRequest.ShouldNotBeNull().ReasoningEffort.Name.ShouldBe("Low");

        await model.Reply(TestRequests.Text("High"), TestContext.Current.CancellationToken);
        model.LastRequest.ShouldNotBeNull().ReasoningEffort.Name.ShouldBe("High");
        model.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task AnEffortTheRowDoesNotListIsRefusedBeforeTheCallWithItsOwnCode()
    {
        var configuration = StubLanguageModelConfiguration.Carrying(["Chat"], ["Low", "High"]);
        var model = StubLanguageModel.Replying(configuration, TestRequests.Plain);

        var result = await model.Reply(TestRequests.Text("Medium"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-21004");
        model.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task NoneIsCarriedOnlyByAModelWhoseRowListsIt()
    {
        var withNone = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], ["None", "High"]), TestRequests.Plain);
        var withoutNone = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], ["High"]), TestRequests.Plain);

        (await withNone.Reply(TestRequests.Text("None"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await withoutNone.Reply(TestRequests.Text("None"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
        withoutNone.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task ReasoningTextIsItsOwnKindOfPieceInTheStream()
    {
        var model = StubLanguageModel.Streaming(StubLanguageModelConfiguration.Carrying(["Chat"], ["High"]),
        [
            new ReasoningPieceEvent("thinking"),
            new TextPieceEvent("answer"),
            new CompletedEvent(FinishReasons.ByName("Completed"), new LanguageModelUsage(1, 2), []),
        ]);

        var events = await StreamCollector.Collect(model.Stream(TestRequests.Text("High"), TestContext.Current.CancellationToken));

        events[0].Kind.Name.ShouldBe("ReasoningPiece");
        events[1].Kind.Name.ShouldBe("TextPiece");
    }
}
