
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A part of plain text.</summary>
public sealed class TextPart : LanguageModelMessagePart
{
    /// <summary>Initializes a new instance of the <see cref="TextPart"/> class.</summary>
    /// <param name="text">The text.</param>
    public TextPart(string text)
    {
        Text = text;
    }

    /// <inheritdoc />
    public override IMessagePartKind Kind => MessagePartKinds.ByName("Text");

    /// <summary>Gets the text.</summary>
    public string Text { get; }
}
