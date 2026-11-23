using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

public sealed class LanguageModelBaseTests
{
    private static readonly string[] Efforts = ["Medium"];

    private sealed class TestCommand : IGenericCommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();

        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public string CommandType => nameof(TestCommand);

        public string Category => "Test";
    }

    [Fact]
    public async Task ARowWithoutAModelIdMakesNoCallAndFailsWithItsOwnCode()
    {
        var configuration = StubLanguageModelConfiguration.Carrying(["Chat"], Efforts);
        configuration.ModelId = null;
        var model = StubLanguageModel.Replying(configuration, TestRequests.Plain);

        var result = await model.Reply(TestRequests.Text("Medium"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-20000");
        model.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task ARequestThatStatesNoRoomForOutputIsRefusedBecauseTheCallerStatesTheLimit()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts), TestRequests.Plain);
        var request = new LanguageModelRequest(
            [new LanguageModelMessage(MessageRoles.ByName("User"), [new TextPart("hi")])], [], ReasoningEfforts.ByName("Medium"), 0, null);

        var result = await model.Reply(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-21005");
        model.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AGenericCommandIsRejectedBecauseAModelTakesARequestThroughReplyOrStream()
    {
        IGenericService model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat"], Efforts), TestRequests.Plain);

        var result = await model.Execute(new TestCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain("LANGUAGEMODELS-91000");
    }

    [Fact]
    public void AModelStatesItsNameItsModelIdAndWhatItCarriesFromItsRow()
    {
        var model = StubLanguageModel.Replying(StubLanguageModelConfiguration.Carrying(["Chat", "Vision"], Efforts), TestRequests.Plain);

        model.Name.ShouldBe("Stub");
        model.ModelId.ShouldBe("test-model");
        model.Capabilities.ShouldBe(["Chat", "Vision"]);
    }
}
