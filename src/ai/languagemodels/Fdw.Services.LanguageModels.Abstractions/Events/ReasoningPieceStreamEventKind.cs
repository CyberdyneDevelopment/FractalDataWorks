using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A piece of the reasoning text.</summary>
[TypeOption(typeof(StreamEventKinds), "ReasoningPiece")]
[ExcludeFromCodeCoverage]
public sealed class ReasoningPieceStreamEventKind : StreamEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ReasoningPieceStreamEventKind"/> class.</summary>
    public ReasoningPieceStreamEventKind() : base("ReasoningPiece", false)
    {
    }
}
