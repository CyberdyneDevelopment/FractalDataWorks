using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>An image the provider makes.</summary>
[TypeOption(typeof(HostedToolKinds), "ImageGeneration")]
[ExcludeFromCodeCoverage]
public sealed class ImageGenerationHostedToolKind : HostedToolKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ImageGenerationHostedToolKind"/> class.</summary>
    public ImageGenerationHostedToolKind() : base("ImageGeneration", "ImageGeneration")
    {
    }
}
