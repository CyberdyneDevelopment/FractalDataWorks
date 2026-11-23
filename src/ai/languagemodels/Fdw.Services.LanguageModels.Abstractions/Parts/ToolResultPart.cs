
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A part that is what the host answers a tool call with.</summary>
public sealed class ToolResultPart : LanguageModelMessagePart
{
    /// <summary>Initializes a new instance of the <see cref="ToolResultPart"/> class.</summary>
    /// <param name="callId">The id of the call this answers.</param>
    /// <param name="content">The result, as text.</param>
    /// <param name="isError">Whether the tool failed and the content says how.</param>
    public ToolResultPart(string callId, string content, bool isError)
    {
        CallId = callId;
        Content = content;
        IsError = isError;
    }

    /// <inheritdoc />
    public override IMessagePartKind Kind => MessagePartKinds.ByName("ToolResult");

    /// <summary>Gets the id of the call this answers.</summary>
    public string CallId { get; }

    /// <summary>Gets the result, as text.</summary>
    public string Content { get; }

    /// <summary>Gets a value indicating whether the tool failed and the content says how.</summary>
    public bool IsError { get; }
}
