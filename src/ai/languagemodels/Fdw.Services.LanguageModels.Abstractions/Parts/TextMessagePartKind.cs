using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>Plain text.</summary>
[TypeOption(typeof(MessagePartKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextMessagePartKind : MessagePartKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextMessagePartKind"/> class.</summary>
    public TextMessagePartKind() : base("Text", "Chat")
    {
    }
}
