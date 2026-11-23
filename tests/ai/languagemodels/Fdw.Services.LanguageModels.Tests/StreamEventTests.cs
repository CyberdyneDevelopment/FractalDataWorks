using Fdw.Results;
using Fdw.Services.LanguageModels.Abstractions;
using Fdw.Services.LanguageModels.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Acceptance criterion 4: stream events: text piece, reasoning piece, tool call, hosted tool activity, completed and failed.</summary>
public sealed class StreamEventTests
{
    private static readonly string[] Efforts = ["Medium"];

    [Fact]
    public async Task AStreamCarriesEveryKindOfPieceAndEndsWithExactlyOneEndingEvent()
    {
        var model = StubLanguageModel.Streaming(StubLanguageModelConfiguration.Carrying(["Chat", "WebSearch"], Efforts),
        [
            new TextPieceEvent("a"),
            new ReasoningPieceEvent("b"),
            new ToolCallEvent(new ToolCallPart("c1", "lookup", "{}")),
            new HostedToolActivityEvent(new HostedToolActivity(HostedToolKinds.ByName("WebSearch"), "searched", "found")),
            new CompletedEvent(FinishReasons.ByName("ToolCalls"), new LanguageModelUsage(3, 4), []),
        ]);

        var events = await StreamCollector.Collect(model.Stream(TestRequests.Text("Medium"), TestContext.Current.CancellationToken));

        events.Select(e => e.Kind.Name).ShouldBe(["TextPiece", "ReasoningPiece", "ToolCall", "HostedToolActivity", "Completed"]);
        events.Count(e => e.Kind.EndsStream).ShouldBe(1);
        events[^1].Kind.EndsStream.ShouldBeTrue();
    }

    [Fact]
    public async Task AFailedStreamEndsWithAFailedEventThatCarriesTheCodedMessage()
    {
        var failure = GenericResult.Failure(LanguageModelLog.RateLimited(NullLogger.Instance, "Stub"));
        var model = StubLanguageModel.Streaming(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts),
        [
            new TextPieceEvent("a"),
            new FailedEvent(failure),
        ]);

        var events = await StreamCollector.Collect(model.Stream(TestRequests.Text("Medium"), TestContext.Current.CancellationToken));

        var last = events[^1].ShouldBeOfType<FailedEvent>();
        last.Kind.EndsStream.ShouldBeTrue();
        last.Failure.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-80001");
    }

    [Fact]
    public async Task ARefusedRequestIsAStreamOfOneFailedEventAndNothingIsCalled()
    {
        var model = StubLanguageModel.Streaming(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts),
        [
            new CompletedEvent(FinishReasons.ByName("Completed"), new LanguageModelUsage(1, 1), []),
        ]);
        var request = TestRequests.Asking("Medium", [new ImagePart("image/png", [1])], []);

        var events = await StreamCollector.Collect(model.Stream(request, TestContext.Current.CancellationToken));

        events.ShouldHaveSingleItem().ShouldBeOfType<FailedEvent>().Failure.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-21003");
        model.Calls.ShouldBe(0);
    }
}
