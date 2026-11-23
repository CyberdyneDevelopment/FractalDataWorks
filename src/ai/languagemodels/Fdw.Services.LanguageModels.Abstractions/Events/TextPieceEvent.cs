
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A piece of the reply text.</summary>
public sealed class TextPieceEvent : LanguageModelStreamEvent
{
    /// <summary>Initializes a new instance of the <see cref="TextPieceEvent"/> class.</summary>
    /// <param name="text">The piece of text.</param>
    public TextPieceEvent(string text)
    {
        Text = text;
    }

    /// <inheritdoc />
    public override IStreamEventKind Kind => StreamEventKinds.ByName("TextPiece");

    /// <summary>Gets the piece of text.</summary>
    public string Text { get; }
}
