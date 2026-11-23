using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The model takes images.</summary>
[TypeOption(typeof(ModelCapabilities), "Vision")]
[ExcludeFromCodeCoverage]
public sealed class VisionModelCapability : ModelCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="VisionModelCapability"/> class.</summary>
    public VisionModelCapability() : base("Vision")
    {
    }
}
