using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

public sealed class LanguageModelCollectionsTests
{
    [Fact]
    public void ReasoningEffortsAreNoneLowMediumHighAndExtraHigh()
        => AllPresent(ReasoningEfforts.ByName, ReasoningEfforts.NotFound, "None", "Low", "Medium", "High", "ExtraHigh");

    [Fact]
    public void FinishReasonsAreCompletedLengthContentFilterAndToolCalls()
        => AllPresent(FinishReasons.ByName, FinishReasons.NotFound, "Completed", "Length", "ContentFilter", "ToolCalls");

    [Fact]
    public void MessagePartKindsAreTextFileImageToolCallAndToolResult()
        => AllPresent(MessagePartKinds.ByName, MessagePartKinds.NotFound, "Text", "File", "Image", "ToolCall", "ToolResult");

    [Fact]
    public void StreamEventKindsAreTextReasoningToolCallHostedToolActivityCompletedAndFailed()
        => AllPresent(StreamEventKinds.ByName, StreamEventKinds.NotFound, "TextPiece", "ReasoningPiece", "ToolCall", "HostedToolActivity", "Completed", "Failed");

    [Fact]
    public void ModelCapabilitiesCarryTheSixOptionsOfTheStory()
        => AllPresent(ModelCapabilities.ByName, ModelCapabilities.NotFound, "Chat", "Vision", "ImageGeneration", "WebSearch", "Transcription", "Speaking");

    [Fact]
    public void ToolExecutorsAreHostAndProviderAndOnlyTheHostRunsInTheHost()
    {
        AllPresent(ToolExecutors.ByName, ToolExecutors.NotFound, "Host", "Provider");
        ToolExecutors.ByName("Host").RunsInHost.ShouldBeTrue();
        ToolExecutors.ByName("Provider").RunsInHost.ShouldBeFalse();
    }

    [Fact]
    public void OnlyToolCallsContinueWithToolResults()
    {
        FinishReasons.ByName("ToolCalls").ContinuesWithToolResults.ShouldBeTrue();
        FinishReasons.ByName("Completed").ContinuesWithToolResults.ShouldBeFalse();
        FinishReasons.ByName("Length").ContinuesWithToolResults.ShouldBeFalse();
        FinishReasons.ByName("ContentFilter").ContinuesWithToolResults.ShouldBeFalse();
    }

    [Fact]
    public void OnlyCompletedAndFailedEndAStream()
    {
        StreamEventKinds.ByName("Completed").EndsStream.ShouldBeTrue();
        StreamEventKinds.ByName("Failed").EndsStream.ShouldBeTrue();
        StreamEventKinds.ByName("TextPiece").EndsStream.ShouldBeFalse();
        StreamEventKinds.ByName("ReasoningPiece").EndsStream.ShouldBeFalse();
        StreamEventKinds.ByName("ToolCall").EndsStream.ShouldBeFalse();
        StreamEventKinds.ByName("HostedToolActivity").EndsStream.ShouldBeFalse();
    }

    [Fact]
    public void EveryCapabilityAPartKindOrAHostedToolKindNeedsIsAnOptionOfModelCapabilities()
    {
        foreach (var name in new[] { "Text", "File", "Image", "ToolCall", "ToolResult" })
            ModelCapabilities.ByName(MessagePartKinds.ByName(name).RequiredCapability).ShouldNotBeSameAs(ModelCapabilities.NotFound);

        foreach (var name in new[] { "WebSearch", "ImageGeneration" })
            ModelCapabilities.ByName(HostedToolKinds.ByName(name).RequiredCapability).ShouldNotBeSameAs(ModelCapabilities.NotFound);
    }

    private static void AllPresent<T>(Func<string, T> byName, T notFound, params string[] names)
        where T : class
    {
        foreach (var name in names)
            byName(name).ShouldNotBeSameAs(notFound, name);
    }
}
