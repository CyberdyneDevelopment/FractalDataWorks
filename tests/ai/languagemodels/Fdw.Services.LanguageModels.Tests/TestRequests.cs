using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Builds the requests and replies the tests send and script.</summary>
internal static class TestRequests
{
    public const int OutputLimit = 256;

    public static LanguageModelRequest Asking(string effort, IReadOnlyList<LanguageModelMessagePart> parts, IReadOnlyList<LanguageModelToolDefinition> tools)
        => new(
            [new LanguageModelMessage(MessageRoles.ByName("User"), parts)],
            tools,
            ReasoningEfforts.ByName(effort),
            OutputLimit,
            null);

    public static LanguageModelRequest Text(string effort)
        => Asking(effort, [new TextPart("Hello")], []);

    public static LanguageModelReply Reply(IReadOnlyList<LanguageModelMessagePart> parts, string finishReason)
        => new(parts, string.Empty, [], [], FinishReasons.ByName(finishReason), new LanguageModelUsage(10, 5));

    public static LanguageModelReply Plain(LanguageModelRequest request)
        => Reply([new TextPart("Hi")], "Completed");
}
