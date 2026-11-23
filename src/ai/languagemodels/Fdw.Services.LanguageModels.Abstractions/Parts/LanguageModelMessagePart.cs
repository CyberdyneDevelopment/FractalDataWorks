namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// One part of a message: text, a file, an image, a tool call or a tool result. Each part states its own kind.
/// </summary>
public abstract class LanguageModelMessagePart
{
    /// <summary>Gets the kind of this part.</summary>
    public abstract IMessagePartKind Kind { get; }
}
