
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A piece of the reasoning text.</summary>
public sealed class ReasoningPieceEvent : LanguageModelStreamEvent
{
    /// <summary>Initializes a new instance of the <see cref="ReasoningPieceEvent"/> class.</summary>
    /// <param name="text">The piece of text.</param>
    public ReasoningPieceEvent(string text)
    {
        Text = text;
    }

    /// <inheritdoc />
    public override IStreamEventKind Kind => StreamEventKinds.ByName("ReasoningPiece");

    /// <summary>Gets the piece of text.</summary>
    public string Text { get; }
}
