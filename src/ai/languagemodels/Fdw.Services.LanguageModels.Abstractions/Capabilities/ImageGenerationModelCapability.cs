using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The model makes images.</summary>
[TypeOption(typeof(ModelCapabilities), "ImageGeneration")]
[ExcludeFromCodeCoverage]
public sealed class ImageGenerationModelCapability : ModelCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="ImageGenerationModelCapability"/> class.</summary>
    public ImageGenerationModelCapability() : base("ImageGeneration")
    {
    }
}
