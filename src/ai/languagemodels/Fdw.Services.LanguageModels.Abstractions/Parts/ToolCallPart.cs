
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A part that is a call the model asks the host to run.</summary>
public sealed class ToolCallPart : LanguageModelMessagePart
{
    /// <summary>Initializes a new instance of the <see cref="ToolCallPart"/> class.</summary>
    /// <param name="callId">The id the model gave this call, which its result carries back.</param>
    /// <param name="toolName">The name of the tool to run.</param>
    /// <param name="argumentsJson">The arguments the model asks the tool to run with, as a JSON document.</param>
    public ToolCallPart(string callId, string toolName, string argumentsJson)
    {
        CallId = callId;
        ToolName = toolName;
        ArgumentsJson = argumentsJson;
    }

    /// <inheritdoc />
    public override IMessagePartKind Kind => MessagePartKinds.ByName("ToolCall");

    /// <summary>Gets the id the model gave this call.</summary>
    public string CallId { get; }

    /// <summary>Gets the name of the tool to run.</summary>
    public string ToolName { get; }

    /// <summary>Gets the arguments the model asks the tool to run with, as a JSON document.</summary>
    public string ArgumentsJson { get; }
}
