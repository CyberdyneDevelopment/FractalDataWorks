using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A piece of the reply text.</summary>
[TypeOption(typeof(StreamEventKinds), "TextPiece")]
[ExcludeFromCodeCoverage]
public sealed class TextPieceStreamEventKind : StreamEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextPieceStreamEventKind"/> class.</summary>
    public TextPieceStreamEventKind() : base("TextPiece", false)
    {
    }
}
