using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Acceptance criteria 5, 6 and 7: finish reasons, token usage per call, and the sources a reply cites.</summary>
public sealed class ReplyShapeTests
{
    private static readonly string[] Efforts = ["Medium"];

    [Theory]
    [InlineData("Completed")]
    [InlineData("Length")]
    [InlineData("ContentFilter")]
    [InlineData("ToolCalls")]
    public async Task EachFinishReasonComesBackAsTheProviderStatedIt(string finishReason)
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts),
            _ => TestRequests.Reply([new TextPart("x")], finishReason));

        var result = await model.Reply(TestRequests.Text("Medium"), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().FinishReason.Name.ShouldBe(finishReason);
    }

    [Fact]
    public async Task UsageCarriesInputAndOutputTokensSoAConsumerCanBudgetATurn()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts),
            _ => new LanguageModelReply([new TextPart("x")], string.Empty, [], [], FinishReasons.ByName("Completed"), new LanguageModelUsage(1200, 340)));

        var result = await model.Reply(TestRequests.Text("Medium"), TestContext.Current.CancellationToken);

        var usage = result.Value.ShouldNotBeNull().Usage;
        usage.InputTokens.ShouldBe(1200);
        usage.OutputTokens.ShouldBe(340);
    }

    [Fact]
    public async Task ACompletedStreamCarriesTheUsageAndTheSources()
    {
        var model = StubLanguageModel.Streaming(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts),
        [
            new CompletedEvent(FinishReasons.ByName("Completed"), new LanguageModelUsage(7, 9), [new LanguageModelSource("Title", "https://example.invalid/a", "cited")]),
        ]);

        var events = await StreamCollector.Collect(model.Stream(TestRequests.Text("Medium"), TestContext.Current.CancellationToken));

        var completed = events.ShouldHaveSingleItem().ShouldBeOfType<CompletedEvent>();
        completed.Usage.OutputTokens.ShouldBe(9);
        completed.Sources.ShouldHaveSingleItem().CitedText.ShouldBe("cited");
    }

    [Fact]
    public async Task ARepliesSourcesAndCitationsComeBackWithIt()
    {
        var source = new LanguageModelSource("The title", "https://example.invalid/page", "the cited text");
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts),
            _ => new LanguageModelReply([new TextPart("x")], string.Empty, [], [source], FinishReasons.ByName("Completed"), new LanguageModelUsage(1, 1)));

        var result = await model.Reply(TestRequests.Text("Medium"), TestContext.Current.CancellationToken);

        var cited = result.Value.ShouldNotBeNull().Sources.ShouldHaveSingleItem();
        cited.Title.ShouldBe("The title");
        cited.Address.ShouldBe("https://example.invalid/page");
        cited.CitedText.ShouldBe("the cited text");
    }
}
