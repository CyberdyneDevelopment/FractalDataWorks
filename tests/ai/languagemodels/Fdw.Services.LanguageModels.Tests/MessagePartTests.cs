using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Acceptance criterion 3: messages made of parts: text, file, image, tool call and tool result.</summary>
public sealed class MessagePartTests
{
    [Fact]
    public void EachPartStatesItsOwnKind()
    {
        new TextPart("t").Kind.Name.ShouldBe("Text");
        new FilePart("application/pdf", [1]).Kind.Name.ShouldBe("File");
        new ImagePart("image/png", [1]).Kind.Name.ShouldBe("Image");
        new ToolCallPart("c1", "lookup", "{}").Kind.Name.ShouldBe("ToolCall");
        new ToolResultPart("c1", "found", false).Kind.Name.ShouldBe("ToolResult");
    }

    [Fact]
    public async Task AMessageOfAllFiveKindsReachesAModelThatCarriesWhatTheyNeed()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat", "Vision"], ["Medium"]), TestRequests.Plain);
        var request = TestRequests.Asking(
            "Medium",
            [new TextPart("look"), new FilePart("application/pdf", [1]), new ImagePart("image/png", [1]), new ToolCallPart("c1", "lookup", "{}"), new ToolResultPart("c1", "found", false)],
            []);

        var result = await model.Reply(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        model.LastRequest.ShouldNotBeNull().Messages[0].Parts.Select(p => p.Kind.Name)
            .ShouldBe(["Text", "File", "Image", "ToolCall", "ToolResult"]);
    }

    [Fact]
    public async Task AReplyIsMadeOfPartsAndAToolCallIsOneOfThem()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], ["Medium"]),
            _ => TestRequests.Reply([new TextPart("one moment"), new ToolCallPart("c1", "lookup", "{\"q\":1}")], "ToolCalls"));

        var result = await model.Reply(TestRequests.Text("Medium"), TestContext.Current.CancellationToken);

        var reply = result.Value.ShouldNotBeNull();
        reply.Parts.Select(p => p.Kind.Name).ShouldBe(["Text", "ToolCall"]);
        reply.FinishReason.ContinuesWithToolResults.ShouldBeTrue();
    }
}
