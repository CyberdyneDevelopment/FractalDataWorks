using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Acceptance criterion 8: what a model carries is data on its row, and a request it cannot carry is refused before any call, with a coded refusal.</summary>
public sealed class CapabilityRefusalTests
{
    private static readonly string[] Efforts = ["Medium"];

    private static LanguageModelRequest WithImage()
        => TestRequests.Asking("Medium", [new TextPart("what is this"), new ImagePart("image/png", [1, 2])], []);

    private static LanguageModelRequest WithWebSearch()
        => TestRequests.Asking("Medium", [new TextPart("news")], [LanguageModelToolDefinition.RunByProvider(HostedToolKinds.ByName("WebSearch"))]);

    [Fact]
    public async Task AnImageToAModelWithoutVisionIsRefusedBeforeTheCall()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts), TestRequests.Plain);

        var result = await model.Reply(WithImage(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-21003");
        model.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AnImageToAModelThatListsVisionIsCarried()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat", "Vision"], Efforts), TestRequests.Plain);

        var result = await model.Reply(WithImage(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        model.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AHostedWebSearchToAModelWithoutWebSearchIsRefusedBeforeTheCall()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts), TestRequests.Plain);

        var result = await model.Reply(WithWebSearch(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-21003");
        model.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AHostedImageGenerationToAModelWithoutItIsRefusedBeforeTheCall()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat", "WebSearch"], Efforts), TestRequests.Plain);
        var request = TestRequests.Asking("Medium", [new TextPart("draw")], [LanguageModelToolDefinition.RunByProvider(HostedToolKinds.ByName("ImageGeneration"))]);

        var result = await model.Reply(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        model.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AModelWithoutChatCarriesNoRequestAtAll()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Vision"], Efforts), TestRequests.Plain);

        var result = await model.Reply(TestRequests.Text("Medium"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        model.Calls.ShouldBe(0);
    }
}
