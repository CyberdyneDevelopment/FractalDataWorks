namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>One event of a streamed reply. A stream ends with exactly one event whose kind ends the stream: completed or failed.</summary>
public abstract class LanguageModelStreamEvent
{
    /// <summary>Gets the kind of this event.</summary>
    public abstract IStreamEventKind Kind { get; }
}
