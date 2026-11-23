using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>An image.</summary>
[TypeOption(typeof(MessagePartKinds), "Image")]
[ExcludeFromCodeCoverage]
public sealed class ImageMessagePartKind : MessagePartKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ImageMessagePartKind"/> class.</summary>
    public ImageMessagePartKind() : base("Image", "Vision")
    {
    }
}
