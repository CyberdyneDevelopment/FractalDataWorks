using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Acceptance criterion 1: hosted tools the provider runs itself, declared beside the host's own tools, with their activity and results in the stream.</summary>
public sealed class HostedToolTests
{
    private static readonly string[] Efforts = ["Medium"];

    private static LanguageModelRequest AskWithBothKindsOfTool()
        => TestRequests.Asking(
            "Medium",
            [new TextPart("Find the weather")],
            [
                LanguageModelToolDefinition.RunByHost("lookup_city", "Finds a city", "{\"type\":\"object\"}"),
                LanguageModelToolDefinition.RunByProvider(HostedToolKinds.ByName("WebSearch")),
            ]);

    [Fact]
    public void ADefinitionStatesWhetherTheHostOrTheProviderRunsTheTool()
    {
        var tools = AskWithBothKindsOfTool().Tools;

        tools[0].Executor.RunsInHost.ShouldBeTrue();
        tools[0].HostedKind.ShouldBeNull();
        tools[0].InputSchema.ShouldNotBeNull();
        tools[1].Executor.RunsInHost.ShouldBeFalse();
        tools[1].HostedKind.ShouldNotBeNull().Name.ShouldBe("WebSearch");
        tools[1].InputSchema.ShouldBeNull();
    }

    [Fact]
    public async Task HostedToolsAreDeclaredBesideHostToolsAndTheirActivityComesBackInTheReply()
    {
        var configuration = StubLanguageModelConfiguration.Carrying(["Chat", "WebSearch"], Efforts);
        var activity = new HostedToolActivity(HostedToolKinds.ByName("WebSearch"), "searched for the weather", "sunny");
        var model = StubLanguageModel.Replying(configuration, _ => new LanguageModelReply(
            [new TextPart("It is sunny")], string.Empty, [activity], [], FinishReasons.ByName("Completed"), new LanguageModelUsage(1, 1)));

        var result = await model.Reply(AskWithBothKindsOfTool(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        model.LastRequest.ShouldNotBeNull().Tools.Count.ShouldBe(2);
        var reply = result.Value.ShouldNotBeNull();
        reply.HostedToolActivities.ShouldHaveSingleItem().Result.ShouldBe("sunny");
    }

    [Fact]
    public async Task HostedToolActivityAndItsResultAreEventsOfTheStream()
    {
        var configuration = StubLanguageModelConfiguration.Carrying(["Chat", "WebSearch"], Efforts);
        var running = new HostedToolActivity(HostedToolKinds.ByName("WebSearch"), "searching", null);
        var found = new HostedToolActivity(HostedToolKinds.ByName("WebSearch"), "searched", "sunny");
        var model = StubLanguageModel.Streaming(configuration,
        [
            new HostedToolActivityEvent(running),
            new HostedToolActivityEvent(found),
            new CompletedEvent(FinishReasons.ByName("Completed"), new LanguageModelUsage(1, 1), []),
        ]);

        var events = await StreamCollector.Collect(model.Stream(AskWithBothKindsOfTool(), TestContext.Current.CancellationToken));

        events.OfType<HostedToolActivityEvent>().Select(e => e.Activity.Result).ShouldBe([null, "sunny"]);
    }

    [Fact]
    public async Task ImageGenerationIsAHostedToolARowThatListsItCarries()
    {
        var configuration = StubLanguageModelConfiguration.Carrying(["Chat", "ImageGeneration"], Efforts);
        var drawn = new HostedToolActivity(HostedToolKinds.ByName("ImageGeneration"), "drew a cat", "image-1");
        var model = StubLanguageModel.Replying(configuration, _ => new LanguageModelReply(
            [new TextPart("Here is a cat")], string.Empty, [drawn], [], FinishReasons.ByName("Completed"), new LanguageModelUsage(1, 1)));
        var request = TestRequests.Asking("Medium", [new TextPart("Draw a cat")], [LanguageModelToolDefinition.RunByProvider(HostedToolKinds.ByName("ImageGeneration"))]);

        var result = await model.Reply(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().HostedToolActivities.ShouldHaveSingleItem().Kind.Name.ShouldBe("ImageGeneration");
    }
}
