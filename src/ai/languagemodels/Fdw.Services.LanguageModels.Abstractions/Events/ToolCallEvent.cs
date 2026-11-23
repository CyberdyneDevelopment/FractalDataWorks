
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A call the model asks the host to run.</summary>
public sealed class ToolCallEvent : LanguageModelStreamEvent
{
    /// <summary>Initializes a new instance of the <see cref="ToolCallEvent"/> class.</summary>
    /// <param name="call">The call.</param>
    public ToolCallEvent(ToolCallPart call)
    {
        Call = call;
    }

    /// <inheritdoc />
    public override IStreamEventKind Kind => StreamEventKinds.ByName("ToolCall");

    /// <summary>Gets the call.</summary>
    public ToolCallPart Call { get; }
}
